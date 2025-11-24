using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConduitNet.Contracts;
using ConduitNet.Core;

namespace ConduitNet.Telemetry.Node
{
    public class TelemetryCollectorService : ITelemetryCollector
    {
        // Simple in-memory storage
        private static readonly ConcurrentBag<TraceSpanDto> _spans = new();

        public Task IngestBatchAsync(List<TraceSpanDto> spans)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[TelemetryCollector] Received batch of {spans.Count} spans");
            Console.ResetColor();

            foreach (var span in spans)
            {
                _spans.Add(span);
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

        public Task<List<TraceSpanDto>> GetRecentSpansAsync()
        {
            // Return last 100 spans
            return Task.FromResult(_spans.OrderByDescending(s => s.StartTime).Take(100).ToList());
        }

        public Task<List<TraceSpanDto>> GetTraceAsync(string traceId)
        {
            return Task.FromResult(_spans.Where(s => s.TraceId == traceId).OrderBy(s => s.StartTime).ToList());
        }
    }
}
