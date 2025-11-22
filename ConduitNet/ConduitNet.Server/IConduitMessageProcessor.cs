using System.Threading.Tasks;
using ConduitNet.Core;

namespace ConduitNet.Server
{
    public interface IConduitMessageProcessor
    {
        Task<ConduitMessage> ProcessAsync(ConduitMessage request);
    }
}
