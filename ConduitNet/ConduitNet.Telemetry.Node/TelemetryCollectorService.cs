using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConduitNet.Contracts;
using ConduitNet.Core;

namespace ConduitNet.Telemetry.Node
{
    public class TelemetryCollectorService : ITelemetryCollector
    {
        public Task IngestBatchAsync(List<TraceSpanDto> spans)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[TelemetryCollector] Received batch of {spans.Count} spans");
            Console.ResetColor();

            foreach (var span in spans)
            {
                Console.WriteLine($"  Trace: {span.TraceId} | Span: {span.SpanId} | {span.Name} ({span.Duration.TotalMilliseconds}ms)");
                if (span.Tags.Count > 0)
                {
                    foreach (var tag in span.Tags)
                    {
                        Console.WriteLine($"    - {tag.Key}: {tag.Value}");
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
