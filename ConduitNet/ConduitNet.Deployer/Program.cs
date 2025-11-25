using System;
using System.IO;
using System.Threading.Tasks;
using ConduitNet.Deployment;

namespace ConduitNet.Deployer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("ConduitNet Deployment Tool");
            Console.WriteLine("==========================");

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Assuming we are running from ConduitNet.Deployer/bin/Debug/net9.0
            // We need to go up to solution root to find Examples
            var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            
            var directoryExe = Path.Combine(solutionDir, "ConduitNet", "ConduitNet.System", "ConduitNet.Directory", "bin", "Debug", "net9.0", "ConduitNet.Directory.exe");
            var userServiceExe = Path.Combine(solutionDir, "ConduitNet", "ConduitNet.System", "ConduitNet.UserService", "bin", "Debug", "net9.0", "ConduitNet.UserService.exe");
            var aclServiceExe = Path.Combine(solutionDir, "ConduitNet", "ConduitNet.System", "ConduitNet.AclService", "bin", "Debug", "net9.0", "ConduitNet.AclService.exe");

            if (!File.Exists(directoryExe))
            {
                Console.WriteLine($"Error: Directory executable not found at {directoryExe}");
                Console.WriteLine("Please build the solution first.");
                return;
            }

            var manifest = new DeploymentManifest();

            // 1. Directory Node
            manifest.Nodes.Add(new ServiceNode
            {
                Id = "Directory",
                ServiceName = "ConduitDirectory",
                ExecutablePath = directoryExe,
                Port = 5000
            });

            // 2. User Service Node 1
            manifest.Nodes.Add(new ServiceNode
            {
                Id = "UserService-1",
                ServiceName = "UserService",
                ExecutablePath = userServiceExe,
                Port = 5001,
                EnvironmentVariables = 
                {
                    { "Conduit:DirectoryUrl", "ws://localhost:5000/conduit" }
                }
            });

            // 3. User Service Node 2
            manifest.Nodes.Add(new ServiceNode
            {
                Id = "UserService-2",
                ServiceName = "UserService",
                ExecutablePath = userServiceExe,
                Port = 5002,
                EnvironmentVariables = 
                {
                    { "Conduit:DirectoryUrl", "ws://localhost:5000/conduit" }
                }
            });

            // 4. ACL Service Node
            manifest.Nodes.Add(new ServiceNode
            {
                Id = "AclService",
                ServiceName = "AclService",
                ExecutablePath = aclServiceExe,
                Port = 5003,
                EnvironmentVariables = 
                {
                    { "Conduit:DirectoryUrl", "ws://localhost:5000/conduit" }
                }
            });

            IDeploymentDriver driver = new LocalhostDriver();

            Console.WriteLine("Deploying stack...");
            await driver.DeployAsync(manifest);

            Console.WriteLine("Stack deployed. Press Enter to stop...");
            Console.ReadLine();

            Console.WriteLine("Stopping stack...");
            await driver.StopAsync();
            Console.WriteLine("Done.");
        }
    }
}
