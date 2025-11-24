using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConduitNet.Contracts;
using OpenTelemetry;
using OpenTelemetry.Resources;

namespace ConduitNet.Client
{
    public class ConduitTraceExporter : BaseExporter<Activity>
    {
        private readonly ITelemetryCollector _collector;
        private readonly string _serviceName;
        private readonly string _nodeId;

        public ConduitTraceExporter(ITelemetryCollector collector, string serviceName = "Unknown", string nodeId = "")
        {
            _collector = collector;
            _serviceName = serviceName;
            _nodeId = nodeId;
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            var spans = new List<TraceSpanDto>();

            foreach (var activity in batch)
            {
                var span = new TraceSpanDto
                {
                    TraceId = activity.TraceId.ToString(),
                    SpanId = activity.SpanId.ToString(),
                    ParentSpanId = activity.ParentSpanId.ToString() == "0000000000000000" ? null : activity.ParentSpanId.ToString(),
                    Name = activity.DisplayName,
                    Kind = activity.Kind.ToString(),
                    StartTime = activity.StartTimeUtc,
                    Duration = activity.Duration,
                    ServiceName = _serviceName
                };

                foreach (var tag in activity.Tags)
                {
                    span.Tags[tag.Key] = tag.Value?.ToString() ?? "";
                }

                // Ensure service.instance.id is present if we have a NodeId
                if (!string.IsNullOrEmpty(_nodeId))
                {
                    span.Tags["service.instance.id"] = _nodeId;
                }

                spans.Add(span);
            }

            try
            {
                // Fire and forget? Or wait?
                // Export is synchronous, but our client is async.
                // We should probably block or use a background queue, but for now .Wait() is the simplest proof of concept.
                // Ideally, we would use a dedicated background worker, but BaseExporter is designed to be called by a processor.
                _collector.IngestBatchAsync(spans).GetAwaiter().GetResult();
                return ExportResult.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConduitTraceExporter] Failed to export traces: {ex.Message}");
                return ExportResult.Failure;
            }
        }
    }
}
