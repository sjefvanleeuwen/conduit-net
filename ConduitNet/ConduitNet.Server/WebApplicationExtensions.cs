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
            // For now, we just map the middleware to /conduit
            // We use Map to branch the pipeline
            
            app.Map("/conduit", conduitApp => 
            {
                conduitApp.UseMiddleware<ConduitWebSocketMiddleware>();
            });

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

