using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitNet.Server
{
    public class ConduitWebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public ConduitWebSocketMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("This endpoint only accepts WebSocket connections.");
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            
            // Create a handler instance for this connection
            // We use ActivatorUtilities to inject dependencies but also manage state if needed
            // Or just resolve it.
            var handler = _serviceProvider.GetRequiredService<ConduitDispatcher>();
            
            await handler.HandleConnectionAsync(webSocket);
        }
    }
}

