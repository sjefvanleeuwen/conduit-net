using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConduitNet.Deployment
{
    public interface IDeploymentDriver
    {
        Task DeployAsync(DeploymentManifest manifest);
        Task StopAsync();
    }

    public class DeploymentManifest
    {
        public List<ServiceNode> Nodes { get; set; } = new();
    }

    public class ServiceNode
    {
        public string Id { get; set; }
        public string ServiceName { get; set; } // e.g. "ConduitNet.Examples.Directory"
        public string ExecutablePath { get; set; } // For localhost driver
        public int Port { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public List<string> Arguments { get; set; } = new();
    }
}
