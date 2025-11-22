using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.IO;

namespace ConduitNet.Tests.E2E
{
    public class ConsensusRoutingTests : IDisposable
    {
        private Process? _nodeA;
        private Process? _nodeB;
        private Process? _gateway;
        private readonly System.Text.StringBuilder _logs = new();

        [Fact]
        public async Task Gateway_Should_Learn_Leader_And_Redirect()
        {
            // Use DLLs directly from the build output directory (copied via ProjectReference)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            var api1Path = Path.Combine(baseDir, "ConduitNet.Examples.Directory.dll");
            var api2Path = Path.Combine(baseDir, "ConduitNet.Examples.UserService.dll");

            if (!File.Exists(api1Path)) throw new FileNotFoundException($"Api1 not found at {api1Path}");
            if (!File.Exists(api2Path)) throw new FileNotFoundException($"Api2 not found at {api2Path}");

            // 1. Start Gateway (Api1 - Directory) on 5000
            _gateway = StartProcess(api1Path, "--urls http://localhost:5000");

            // 2. Start Node B (Leader) on 5003
            // Note: We use --urls to override the port
            _nodeB = StartProcess(api2Path, "--urls http://localhost:5003 --Consensus:IsLeader=true --Conduit:DirectoryUrl=ws://localhost:5000/conduit");

            // 3. Start Node A (Follower) on 5002
            // Configured to redirect to 5003 (via Consensus LeaderUrl) but also register with Directory
            _nodeA = StartProcess(api2Path, "--urls http://localhost:5002 --Consensus:IsLeader=false --Consensus:LeaderUrl=ws://localhost:5003/conduit --Conduit:DirectoryUrl=ws://localhost:5000/conduit");

            // Give them time to start up
            await Task.Delay(5000);
// ...existing code...
        }

        private Process StartProcess(string dllPath, string args)
        {
            // Run with "dotnet [dll] [args]"
            var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\" {args}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(dllPath) ?? string.Empty
            };
            var p = Process.Start(psi);
            
            p!.OutputDataReceived += (s, e) => { if (e.Data != null) lock(_logs) _logs.AppendLine($"[{Path.GetFileName(dllPath)}:{args}] {e.Data}"); };
            p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock(_logs) _logs.AppendLine($"[{Path.GetFileName(dllPath)}:{args} ERROR] {e.Data}"); };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }

        public void Dispose()
        {
            KillProcess(_gateway);
            KillProcess(_nodeA);
            KillProcess(_nodeB);
        }

        private void KillProcess(Process? p)
        {
            try
            {
                if (p != null && !p.HasExited)
                {
                    p.Kill();
                    p.WaitForExit();
                }
            }
            catch {}
        }
    }
}
