using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ConduitNet.Deployment
{
    public class LocalhostDriver : IDeploymentDriver
    {
        private readonly List<Process> _processes = new();

        public Task DeployAsync(DeploymentManifest manifest)
        {
            foreach (var node in manifest.Nodes)
            {
                StartNode(node);
            }
            return Task.CompletedTask;
        }

        private void StartNode(ServiceNode node)
        {
            if (string.IsNullOrEmpty(node.ExecutablePath) || !File.Exists(node.ExecutablePath))
            {
                throw new FileNotFoundException($"Executable not found for node {node.Id}", node.ExecutablePath);
            }

            var args = new List<string>(node.Arguments);
            args.Add($"--urls=http://localhost:{node.Port}");

            var psi = new ProcessStartInfo(node.ExecutablePath)
            {
                UseShellExecute = false,
                CreateNoWindow = false, // Let's keep windows visible for now for debugging, or maybe true if we want them hidden
                WorkingDirectory = Path.GetDirectoryName(node.ExecutablePath)
            };

            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            foreach (var env in node.EnvironmentVariables)
            {
                psi.EnvironmentVariables[env.Key] = env.Value;
            }

            Console.WriteLine($"[LocalhostDriver] Starting {node.Id} on port {node.Port}...");
            var process = Process.Start(psi);
            if (process != null)
            {
                _processes.Add(process);
            }
        }

        public Task StopAsync()
        {
            foreach (var process in _processes)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LocalhostDriver] Error stopping process: {ex.Message}");
                }
            }
            _processes.Clear();
            return Task.CompletedTask;
        }
    }
}
