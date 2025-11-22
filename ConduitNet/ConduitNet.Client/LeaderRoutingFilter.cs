using System.Collections.Concurrent;
using System.Threading.Tasks;
using ConduitNet.Core;

namespace ConduitNet.Client
{
    /// <summary>
    /// A persistent filter that maintains a cache of "Leader" nodes for services.
    /// If a response indicates a redirect (via "X-Conduit-Leader-Redirect" header),
    /// this filter updates the cache and transparently retries the request to the new leader.
    /// </summary>
    public class LeaderRoutingFilter : IConduitFilter
    {
        // Maps InterfaceName -> LeaderUrl
        private readonly ConcurrentDictionary<string, string> _leaderCache = new();

        public async ValueTask<ConduitMessage> InvokeAsync(ConduitMessage message, ConduitDelegate next)
        {
            // 1. Apply cached leader if available
            if (_leaderCache.TryGetValue(message.InterfaceName, out var leaderUrl))
            {
                message.Headers["Target-Url"] = leaderUrl;
            }

            // 2. Execute the request
            var response = await next(message);

            // 3. Check for Redirect Signal
            if (response.Headers.TryGetValue("X-Conduit-Leader-Redirect", out var newLeaderUrl) && !string.IsNullOrEmpty(newLeaderUrl))
            {
                // Update the cache
                _leaderCache[message.InterfaceName] = newLeaderUrl;

                // Update the target for the retry
                message.Headers["Target-Url"] = newLeaderUrl;

                // 4. Retry the request immediately
                // Note: We call 'next' again. Since 'next' is the rest of the pipeline (Transport),
                // it will use the new Target-Url.
                return await next(message);
            }

            return response;
        }
    }
}
