using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitNet.Rpc.Server
{
    public class RpcWebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public RpcWebSocketMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            
            // Create a handler instance for this connection
            // We use ActivatorUtilities to inject dependencies but also manage state if needed
            // Or just resolve it.
            var handler = _serviceProvider.GetRequiredService<RpcRequestHandler>();
            
            await handler.HandleConnectionAsync(webSocket);
        }
    }
}
