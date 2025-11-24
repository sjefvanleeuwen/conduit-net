using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitNet.Server
{
    public static class WebApplicationExtensions
    {
        public static IApplicationBuilder MapConduitService<TInterface>(this IApplicationBuilder app)
        {
            // In a real implementation, we might register the interface type to a list
            // so the handler knows it's allowed.
            // For now, we just map the middleware to the root
            
            app.UseMiddleware<ConduitWebSocketMiddleware>();

            return app;
        }

        public static IServiceCollection AddConduitServer(this IServiceCollection services)
        {
            services.AddTransient<IConduitMessageProcessor, ConduitMessageProcessor>();
            services.AddTransient<ConduitDispatcher>();
            return services;
        }
    }
}

