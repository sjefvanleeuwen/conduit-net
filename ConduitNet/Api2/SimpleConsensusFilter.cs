using System.Threading.Tasks;
using ConduitNet.Core;
using Microsoft.Extensions.Configuration;

namespace Api2
{
    public class SimpleConsensusFilter : IConduitFilter
    {
        private readonly IConfiguration _configuration;

        public SimpleConsensusFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async ValueTask<ConduitMessage> InvokeAsync(ConduitMessage message, ConduitDelegate next)
        {
            var isLeader = _configuration.GetValue<bool>("Consensus:IsLeader");
            var leaderUrl = _configuration.GetValue<string>("Consensus:LeaderUrl");

            // If we are NOT the leader, and we know who IS the leader, redirect.
            if (!isLeader && !string.IsNullOrEmpty(leaderUrl))
            {
                return new ConduitMessage
                {
                    Id = message.Id,
                    IsError = false,
                    Headers = 
                    { 
                        ["X-Conduit-Leader-Redirect"] = leaderUrl 
                    }
                };
            }

            // Otherwise, process the request locally.
            return await next(message);
        }
    }
}
