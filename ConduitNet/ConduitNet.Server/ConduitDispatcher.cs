using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Core;
using System.Reflection;
using System.Linq;

namespace ConduitNet.Server
{
    public class ConduitDispatcher
    {
        private readonly IServiceProvider _provider;
        private readonly IEnumerable<IConduitFilter> _filters;

        public ConduitDispatcher(IServiceProvider provider, IEnumerable<IConduitFilter> filters)
        {
            _provider = provider;
            _filters = filters;
        }

        public async Task HandleConnectionAsync(WebSocket webSocket)
        {
            var pipe = new Pipe();
            var outgoingChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            var fillTask = FillPipeAsync(webSocket, pipe.Writer);
            var readTask = ReadPipeAsync(webSocket, pipe.Reader, outgoingChannel.Writer);
            var sendTask = SendLoopAsync(webSocket, outgoingChannel.Reader);

            await Task.WhenAll(fillTask, readTask, sendTask);
        }

        private async Task SendLoopAsync(WebSocket webSocket, ChannelReader<byte[]> reader)
        {
            try
            {
                while (await reader.WaitToReadAsync())
                {
                    while (reader.TryRead(out var messageBytes))
                    {
                        if (webSocket.State != WebSocketState.Open) break;

                        var lengthPrefix = BitConverter.GetBytes(messageBytes.Length);
                        await webSocket.SendAsync(lengthPrefix, WebSocketMessageType.Binary, false, CancellationToken.None);
                        await webSocket.SendAsync(messageBytes, WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
            }
            catch
            {
                // Ignore send errors
            }
        }

        private async Task FillPipeAsync(WebSocket webSocket, PipeWriter writer)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    Memory<byte> memory = writer.GetMemory(4096);
                    var result = await webSocket.ReceiveAsync(memory, CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    writer.Advance(result.Count);
                    var flushResult = await writer.FlushAsync();
                    
                    if (flushResult.IsCompleted)
                        break;
                }
                catch
                {
                    break;
                }
            }
            await writer.CompleteAsync();
        }

        private async Task ReadPipeAsync(WebSocket webSocket, PipeReader reader, ChannelWriter<byte[]> writer)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadMessage(ref buffer, out ConduitMessage? message))
                {
                    if (message != null)
                    {
                        // Process in background to not block reading
                        _ = ProcessAndRespondAsync(message, writer);
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }
            await reader.CompleteAsync();
        }

        private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ConduitMessage? message)
        {
            if (buffer.Length < 4)
            {
                message = null;
                return false;
            }

            Span<byte> lengthBytes = stackalloc byte[4];
            buffer.Slice(0, 4).CopyTo(lengthBytes);
            int messageLength = BitConverter.ToInt32(lengthBytes);

            if (buffer.Length < 4 + messageLength)
            {
                message = null;
                return false;
            }

            // Zero-copy deserialization
            var messageSequence = buffer.Slice(4, messageLength);
            message = MessagePackSerializer.Deserialize<ConduitMessage>(messageSequence);
            
            buffer = buffer.Slice(4 + messageLength);
            return true;
        }

        private async Task ProcessAndRespondAsync(ConduitMessage request, ChannelWriter<byte[]> writer)
        {
            ConduitMessage response;
            using var scope = _provider.CreateScope();

            try
            {
                // 1. Resolve Metadata & Service
                var interfaceType = Type.GetType($"ConduitNet.Contracts.{request.InterfaceName}, ConduitNet.Contracts");
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
                    pipeline = msg => filter.InvokeAsync(msg, next);
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

            try
            {
                var messageBytes = MessagePackSerializer.Serialize(response);
                await writer.WriteAsync(messageBytes);
            }
            catch {}
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
                else
                {
                    var resultProperty = task.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        result = resultProperty.GetValue(task);
                        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                        {
                            returnType = returnType.GetGenericArguments()[0];
                        }
                    }
                    else
                    {
                        // Should not happen if ReturnType is not Task
                        returnType = typeof(void);
                    }
                }
            }
            else
            {
                result = resultObj;
            }

            byte[] payload;
            if (returnType == typeof(void) || result == null)
            {
                payload = MessagePackSerializer.Serialize<object?>(null);
            }
            else
            {
                payload = MessagePackSerializer.Serialize(returnType, result);
            }

            return new ConduitMessage 
            { 
                Id = request.Id,
                Payload = payload,
                IsError = false
            };
        }
    }
}

