export class DocTelemetry extends HTMLElement {
    connectedCallback() {
        this.innerHTML = `
            <div class="doc-jumbotron" style="background-image: url('/images/background5.png');">
                <h2>The "Everything is a Node" Philosophy</h2>
                <p>In ConduitNet, telemetry isn't a sidecar or a magical agent. It's just another service.</p>
            </div>
            <div class="doc-content">

                <section>
                    <h3>Architecture Overview</h3>
                    <p>Instead of pushing data to an external HTTP endpoint directly, services in the mesh export their traces to a dedicated <code>ITelemetryCollector</code> service. This service runs on a standard Conduit Node.</p>
                    
                    <div class="mermaid">
                        graph TD
                        subgraph "Conduit Mesh"
                            Dir[Directory Service]
                            
                            subgraph "User Node"
                                US[User Service]
                                Exp[Conduit Trace Exporter]
                            end
                            
                            subgraph "Telemetry Node"
                                TC[Telemetry Collector]
                            end
                            
                            US -->|1. Register| Dir
                            TC -->|1. Register| Dir
                            
                            US -- "2. RPC: GetUser" --> US
                            Exp -- "3. RPC: ExportBatch" --> TC
                        end
                        
                        TC -->|4. Forward| OTLP[OpenTelemetry Collector / Jaeger]
                    </div>

                    <hr />

                    <h3>1. The Telemetry Node</h3>
                    <p>The Telemetry Node is a standard Conduit application. It implements the <code>ITelemetryCollector</code> contract. This demonstrates the power of the "Everything is a Node" architecture: infrastructure concerns are implemented using the same primitives as business logic.</p>

                    <pre class="line-numbers"><code class="language-csharp">using ConduitNet.Node;
using ConduitNet.Contracts;

namespace ConduitNet.Telemetry.Node
{
    public class TelemetryNode : ConduitNode
    {
        public TelemetryNode(string[] args) : base(args) { }

        protected override void ConfigureServices(IServiceCollection services)
        {
            // Register the Telemetry Collector Service implementation
            // This makes it discoverable by the Directory
            RegisterConduitService&lt;ITelemetryCollector, TelemetryCollectorService&gt;();
        }

        protected override void Configure(WebApplication app)
        {
            // Expose the Conduit RPC endpoint over WebSockets
            app.MapConduitService&lt;ITelemetryCollector&gt;();
        }
    }
}</code></pre>

                    <h3>2. The Collector Service Implementation</h3>
                    <p>The service implementation receives batches of traces. It can then process them, log them, or forward them to an external system like Jaeger or Honeycomb.</p>

                    <pre class="line-numbers"><code class="language-csharp">public class TelemetryCollectorService : ITelemetryCollector
{
    public Task ExportBatchAsync(List&lt;ActivityExportDto&gt; batch)
    {
        Console.WriteLine($"[TelemetryCollector] Received batch of {batch.Count} spans");
        
        foreach (var span in batch)
        {
            Console.WriteLine($" - Trace: {span.TraceId} | Span: {span.SpanId} | Name: {span.DisplayName}");
        }
        
        return Task.CompletedTask;
    }
}</code></pre>

                    <hr />

                    <h3>3. Configuring Services to Emit Telemetry</h3>
                    <p>Any service in the mesh can be configured to send its traces to the Telemetry Node. We provide a custom <code>ConduitTraceExporter</code> that adapts OpenTelemetry's export pipeline to Conduit's RPC mechanism.</p>

                    <pre class="line-numbers"><code class="language-csharp">// In your Service's ConfigureServices method
protected override void ConfigureServices(IServiceCollection services)
{
    // 1. Register the Client for the Telemetry Collector
    // This allows this node to talk to the Telemetry Node via the Directory
    services.AddConduitClient&lt;ITelemetryCollector&gt;();

    // 2. Configure OpenTelemetry
    services.AddOpenTelemetry()
        .WithTracing(builder => builder
            .AddSource("ConduitNet") // Capture internal framework traces
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyService"))
            .AddProcessor(sp => 
            {
                // Resolve the RPC Client
                var collector = sp.GetRequiredService&lt;ITelemetryCollector&gt;();
                
                // Use the Batch Processor with our Custom RPC Exporter
                return new BatchActivityExportProcessor(
                    new ConduitTraceExporter(collector, "MyService")
                );
            }));
}</code></pre>

                    <h3>Why This Matters</h3>
                    <ul>
                        <li><strong>Protocol Homogeneity:</strong> You don't need to open extra HTTP ports or manage sidecars for telemetry. It travels over the same WebSocket fabric as your application traffic.</li>
                        <li><strong>Dynamic Discovery:</strong> If you move the Telemetry Node to a different server, the Directory automatically updates all services. No config changes required.</li>
                        <li><strong>Resilience:</strong> Since it uses the standard Conduit Client, telemetry export benefits from the same retry policies, load balancing, and circuit breaking as your business logic.</li>
                    </ul>
                </section>
            </div>
        `;
        if ((window as any).Prism) {
            (window as any).Prism.highlightAllUnder(this);
        }
        if ((window as any).mermaid) {
            (window as any).mermaid.init();
        }
    }
}
customElements.define('doc-telemetry', DocTelemetry);
