using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ConduitNet.Core;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitNet.Server
{
    public class ConduitMessageProcessor : IConduitMessageProcessor
    {
        private readonly IServiceProvider _provider;
        private readonly IEnumerable<IConduitFilter> _filters;

        public ConduitMessageProcessor(IServiceProvider provider, IEnumerable<IConduitFilter> filters)
        {
            _provider = provider;
            _filters = filters;
        }

        public async Task<ConduitMessage> ProcessAsync(ConduitMessage request)
        {
            // Extract trace context
            ActivityContext parentContext = default;
            if (request.Headers.TryGetValue("traceparent", out var traceParent))
            {
                ActivityContext.TryParse(traceParent, request.Headers.TryGetValue("tracestate", out var traceState) ? traceState : null, out parentContext);
            }

            using var activity = ConduitTelemetry.Source.StartActivity("ConduitServer.Process", ActivityKind.Server, parentContext);

            ConduitMessage response;
            using var scope = _provider.CreateScope();

            try
            {
                // 1. Resolve Metadata & Service
                var typeName = $"ConduitNet.Contracts.{request.InterfaceName}";
                var interfaceType = Type.GetType($"{typeName}, ConduitNet.Contracts");

                if (interfaceType == null)
                {
                    // Fallback: scan loaded assemblies
                    interfaceType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == typeName || t.Name == request.InterfaceName);
                }

                if (interfaceType == null) throw new Exception($"Interface {request.InterfaceName} not found.");

                var service = scope.ServiceProvider.GetRequiredService(interfaceType);
                var interfaceMethod = interfaceType.GetMethod(request.MethodName);
                if (interfaceMethod == null) throw new Exception($"Method {request.MethodName} not found.");

                // Map to implementation for attributes
                var implementationType = service.GetType();
                var map = implementationType.GetInterfaceMap(interfaceType);
                var targetIndex = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
                var implementationMethod = map.TargetMethods[targetIndex];

                // 2. Collect Filters
                var filters = new List<IConduitFilter>(_filters); // Global filters

                // Attribute filters
                var methodAttrs = implementationMethod.GetCustomAttributes(typeof(ConduitFilterAttribute), true).Cast<ConduitFilterAttribute>();
                var classAttrs = implementationType.GetCustomAttributes(typeof(ConduitFilterAttribute), true).Cast<ConduitFilterAttribute>();
                
                var attrFilters = classAttrs.Concat(methodAttrs)
                    .Select(a => (scope.ServiceProvider.GetService(a.FilterType) ?? ActivatorUtilities.CreateInstance(scope.ServiceProvider, a.FilterType)) as IConduitFilter)
                    .Where(f => f != null);
                
                filters.AddRange(attrFilters!);

                // 3. Build Pipeline
                ConduitDelegate pipeline = async msg => 
                {
                    return await InvokeServiceMethodAsync(msg, service, implementationMethod);
                };

                // Wrap filters (reverse order)
                filters.Reverse();
                foreach (var filter in filters)
                {
                    var next = pipeline;
                    pipeline = async msg => 
                    {
                        using var filterActivity = ConduitTelemetry.Source.StartActivity($"Filter: {filter.GetType().Name}");
                        return await filter.InvokeAsync(msg, next);
                    };
                }

                response = await pipeline(request);
            }
            catch (Exception ex)
            {
                response = new ConduitMessage 
                { 
                    Id = request.Id,
                    Payload = MessagePackSerializer.Serialize(ex.Message),
                    IsError = true
                };
            }

            return response;
        }

        private async ValueTask<ConduitMessage> InvokeServiceMethodAsync(ConduitMessage request, object service, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];
            
            // Use MessagePackReader to deserialize arguments one by one into the correct types
            var sequence = new ReadOnlySequence<byte>(request.Payload);
            var reader = new MessagePackReader(sequence);
            
            int count = reader.ReadArrayHeader();
            
            for (int i = 0; i < count; i++)
            {
                if (i >= parameters.Length) 
                {
                    reader.Skip();
                    continue;
                }

                var start = reader.Position;
                reader.Skip();
                var end = reader.Position;
                
                var argSequence = sequence.Slice(start, end);
                var paramType = parameters[i].ParameterType;
                
                args[i] = MessagePackSerializer.Deserialize(paramType, argSequence);
            }
            
            var resultObj = method.Invoke(service, args);
            object? result = null;
            Type returnType = method.ReturnType;

            if (resultObj is Task task)
            {
                await task.ConfigureAwait(false);
                
                if (returnType == typeof(Task))
                {
                    returnType = typeof(void);
                    result = null;
                }
                else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var resultProperty = task.GetType().GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                    returnType = returnType.GetGenericArguments()[0];
                }
            }
            else
            {
                result = resultObj;
            }

            var responsePayload = result != null 
                ? MessagePackSerializer.Serialize(returnType, result) 
                : MessagePackSerializer.Serialize<object?>(null);

            return new ConduitMessage
            {
                Id = request.Id,
                Payload = responsePayload
            };
        }
    }
}
