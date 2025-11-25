using System.Collections.Concurrent;
using ConduitNet.Contracts;

namespace ConduitNet.Directory
{
    public class ConduitDirectoryService : IConduitDirectory
    {
        // ServiceName -> List of Nodes
        private static readonly ConcurrentDictionary<string, List<NodeInfo>> _registry = new();

        public Task RegisterAsync(NodeInfo node)
        {
            foreach (var service in node.Services)
            {
                _registry.AddOrUpdate(service, 
                    _ => new List<NodeInfo> { node }, 
                    (_, list) => 
                    {
                        lock (list)
                        {
                            list.RemoveAll(n => n.Id == node.Id);
                            list.Add(node);
                        }
                        return list;
                    });
            }
            
            Console.WriteLine($"[Directory] Registered Node {node.Id} at {node.Address} for services: {string.Join(", ", node.Services)}");
            return Task.CompletedTask;
        }

        public Task<List<NodeInfo>> DiscoverAsync(string serviceName)
        {
            if (_registry.TryGetValue(serviceName, out var nodes))
            {
                lock (nodes)
                {
                    return Task.FromResult(nodes.ToList());
                }
            }
            return Task.FromResult(new List<NodeInfo>());
        }
    }
}
