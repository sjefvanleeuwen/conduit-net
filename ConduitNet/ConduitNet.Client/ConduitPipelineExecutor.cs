using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ConduitNet.Core;

namespace ConduitNet.Client
{
    public class ConduitPipelineExecutor
    {
        private readonly ConduitTransport _transport;
        private readonly IEnumerable<IConduitFilter> _filters;

        public ConduitPipelineExecutor(ConduitTransport transport, IEnumerable<IConduitFilter> filters)
        {
            _transport = transport;
            _filters = filters;
        }

        public async Task<ConduitMessage> ExecuteAsync(ConduitMessage message)
        {
            using var activity = ConduitTelemetry.Source.StartActivity("ConduitClient.Execute");

            ConduitDelegate pipeline = async msg => 
            {
                // Inject propagation headers
                if (Activity.Current?.Id != null)
                {
                    msg.Headers["traceparent"] = Activity.Current.Id;
                    if (Activity.Current.TraceStateString != null)
                    {
                        msg.Headers["tracestate"] = Activity.Current.TraceStateString;
                    }
                }
                return await _transport.SendAsync(msg);
            };

            // Build pipeline in reverse order
            var filtersList = new List<IConduitFilter>(_filters);
            filtersList.Reverse();

            foreach (var filter in filtersList)
            {
                var next = pipeline;
                pipeline = async msg => 
                {
                    using var filterActivity = ConduitTelemetry.Source.StartActivity($"Filter: {filter.GetType().Name}");
                    return await filter.InvokeAsync(msg, next);
                };
            }

            return await pipeline(message);
        }
    }
}

