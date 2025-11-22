using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Core;

namespace ConduitNet.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConduitClient<TInterface>(this IServiceCollection services) 
            where TInterface : class
        {
            services.AddSingleton<ConduitTransport>();
            services.AddSingleton<ConduitPipelineExecutor>();
            services.AddSingleton<IConduitFilter, ServiceDiscoveryFilter>();
            // LeaderRoutingFilter is now opt-in via AddConduitLeaderRouting()

            services.AddSingleton<TInterface>(sp => 
            {
                var pipeline = sp.GetRequiredService<ConduitPipelineExecutor>(); 
                
                var proxy = DispatchProxy.Create<TInterface, ConduitProxy<TInterface>>();
                ((ConduitProxy<TInterface>)(object)proxy).Initialize(msg => pipeline.ExecuteAsync(msg));
                
                return proxy;
            });
            return services;
        }

        public static IServiceCollection AddConduitLeaderRouting(this IServiceCollection services)
        {
            services.AddSingleton<IConduitFilter, LeaderRoutingFilter>();
            return services;
        }
    }
}

