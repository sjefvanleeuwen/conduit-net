using System.Threading.Tasks;

namespace ConduitNet.Core
{
    public delegate ValueTask<ConduitMessage> ConduitDelegate(ConduitMessage message);

    public interface IConduitFilter
    {
        ValueTask<ConduitMessage> InvokeAsync(ConduitMessage message, ConduitDelegate next);
    }
}

