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
            // Locate executables relative to the test bin folder
            // Current: ConduitNet\ConduitNet.Tests.E2E\bin\Debug\net9.0
            // Target: ConduitNet\Api1\bin\Debug\net9.0\Api1.exe
            
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            
            var api1Path = Path.Combine(solutionDir, "..", "Examples", "ConduitNet.Examples.Directory", "bin", "Debug", "net9.0", "ConduitNet.Examples.Directory.exe");
            var api2Path = Path.Combine(solutionDir, "..", "Examples", "ConduitNet.Examples.UserService", "bin", "Debug", "net9.0", "ConduitNet.Examples.UserService.exe");

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

            try 
            {
                // 4. Call Gateway
                using var client = new HttpClient();
                var response = await client.GetAsync("http://localhost:5000/trigger");
                var content = await response.Content.ReadAsStringAsync();

                // Always print logs for visibility
                Console.WriteLine("=== TEST LOGS ===");
                Console.WriteLine(_logs.ToString());
                Console.WriteLine("=================");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Request failed: {response.StatusCode} - {content}");
                }

                // 5. Assert
                Assert.True(response.IsSuccessStatusCode, $"Request failed: {response.StatusCode} - {content}");
                Assert.Contains("RPC Call Completed", content);
            }
            catch (Exception)
            {
                // Logs already printed above
                throw;
            }
            finally
            {
                // Cleanup happens in Dispose
            }
        }

        private Process StartProcess(string path, string args)
        {
            var psi = new ProcessStartInfo(path, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(path)
            };
            var p = Process.Start(psi);
            
            p!.OutputDataReceived += (s, e) => { if (e.Data != null) lock(_logs) _logs.AppendLine($"[{Path.GetFileName(path)}:{args}] {e.Data}"); };
            p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock(_logs) _logs.AppendLine($"[{Path.GetFileName(path)}:{args} ERROR] {e.Data}"); };
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
