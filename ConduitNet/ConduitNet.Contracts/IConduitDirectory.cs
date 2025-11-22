using MessagePack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConduitNet.Contracts
{
    public interface IConduitDirectory
    {
        Task RegisterAsync(NodeInfo node);
        Task<List<NodeInfo>> DiscoverAsync(string serviceName);
    }

    [MessagePackObject]
    public class NodeInfo
    {
        [Key(0)] public required string Id { get; set; }
        [Key(1)] public required string Address { get; set; }
        [Key(2)] public List<string> Services { get; set; } = new();
    }
}
