using System;
using System.Reflection;
using System.Threading.Tasks;
using MessagePack;
using ConduitNet.Core;

namespace ConduitNet.Client
{
    public class ConduitProxy<T> : DispatchProxy
    {
        private Func<ConduitMessage, Task<ConduitMessage>> _sendFunc = default!;

        public void Initialize(Func<ConduitMessage, Task<ConduitMessage>> sendFunc)
        {
            _sendFunc = sendFunc;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;

            var message = new ConduitMessage
            {
                InterfaceName = typeof(T).Name,
                MethodName = targetMethod.Name,
                Payload = MessagePackSerializer.Serialize(args ?? Array.Empty<object>())
            };

            var responseTask = _sendFunc(message);

            var returnType = targetMethod.ReturnType;
            
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GetGenericArguments()[0];
                // Use typeof(ConduitProxy<T>) instead of GetType() because GetType() returns the generated proxy class
                var method = typeof(ConduitProxy<T>).GetMethod(nameof(ConvertToGenericTask), BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null) throw new InvalidOperationException("Method ConvertToGenericTask not found.");
                
                return method.MakeGenericMethod(resultType)
                    .Invoke(this, new object[] { responseTask });
            }
            
            if (returnType == typeof(Task))
            {
                return HandleVoidTask(responseTask);
            }

            // Synchronous call (not recommended but handled)
            var response = responseTask.GetAwaiter().GetResult();
            if (response.IsError) throw new Exception(MessagePackSerializer.Deserialize<string>(response.Payload));
            return null;
        }

        private async Task HandleVoidTask(Task<ConduitMessage> task)
        {
            var response = await task;
            if (response.IsError) throw new Exception(MessagePackSerializer.Deserialize<string>(response.Payload));
        }

        private async Task<TResult?> ConvertToGenericTask<TResult>(Task<ConduitMessage> task)
        {
            var response = await task;
            if (response.IsError) 
                throw new Exception(MessagePackSerializer.Deserialize<string>(response.Payload));
                
            return MessagePackSerializer.Deserialize<TResult>(response.Payload);
        }
    }
}

