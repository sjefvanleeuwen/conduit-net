using System.Diagnostics;

namespace ConduitNet.Core
{
    public static class ConduitTelemetry
    {
        public static readonly ActivitySource Source = new("ConduitNet");
    }
}
