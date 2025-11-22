using System;
using System.Collections.Generic;
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
            ConduitDelegate pipeline = msg => new ValueTask<ConduitMessage>(_transport.SendAsync(msg));

            // Build pipeline in reverse order
            var filtersList = new List<IConduitFilter>(_filters);
            filtersList.Reverse();

            foreach (var filter in filtersList)
            {
                var next = pipeline;
                pipeline = msg => filter.InvokeAsync(msg, next);
            }

            return await pipeline(message);
        }
    }
}

