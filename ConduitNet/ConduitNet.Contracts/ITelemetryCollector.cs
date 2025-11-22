using MessagePack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConduitNet.Contracts
{
    public interface ITelemetryCollector
    {
        Task IngestBatchAsync(List<TraceSpanDto> spans);
    }

    [MessagePackObject]
    public class TraceSpanDto
    {
        [Key(0)] public string TraceId { get; set; }
        [Key(1)] public string SpanId { get; set; }
        [Key(2)] public string? ParentSpanId { get; set; }
        [Key(3)] public string Name { get; set; }
        [Key(4)] public string Kind { get; set; }
        [Key(5)] public DateTimeOffset StartTime { get; set; }
        [Key(6)] public TimeSpan Duration { get; set; }
        [Key(7)] public string ServiceName { get; set; }
        [Key(8)] public Dictionary<string, string> Tags { get; set; } = new();
    }
}
