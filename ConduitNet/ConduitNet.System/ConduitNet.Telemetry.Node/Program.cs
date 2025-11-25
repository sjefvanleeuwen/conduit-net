using System;
using Microsoft.Extensions.DependencyInjection;
using ConduitNet.Node;
using ConduitNet.Contracts;
using ConduitNet.Server;

namespace ConduitNet.Telemetry.Node
{
    class Program
    {
        static void Main(string[] args)
        {
            new TelemetryNode(args).Run();
        }
    }

    public class TelemetryNode : ConduitNode
    {
        public TelemetryNode(string[] args) : base(args, "TelemetryService", enableTelemetry: false) { }

        protected override void ConfigureServices(IServiceCollection services)
        {
            // Register the Telemetry Collector Service
            RegisterConduitService<ITelemetryCollector, TelemetryCollectorService>();
        }

        protected override void Configure(Microsoft.AspNetCore.Builder.WebApplication app)
        {
            app.MapConduitService<ITelemetryCollector>();
        }
    }
}
