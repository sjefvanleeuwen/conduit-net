using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using ConduitNet.Client;
using ConduitNet.Contracts;
using ConduitNet.Core;
using System.Collections.Generic;

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
            
            var api1Path = Path.Combine(baseDir, "ConduitNet.Directory.dll");
            var api2Path = Path.Combine(baseDir, "ConduitNet.UserService.dll");

            if (!File.Exists(api1Path)) throw new FileNotFoundException($"Api1 not found at {api1Path}");
            if (!File.Exists(api2Path)) throw new FileNotFoundException($"Api2 not found at {api2Path}");

            // 1. Start Gateway (Api1 - Directory) on 7000
            _gateway = StartProcess(api1Path, "--Conduit:Port 7000");

            // 2. Start Node B (Leader) on 7003
            // Note: We use --Conduit:Port to override the port
            _nodeB = StartProcess(api2Path, "--Conduit:Port 7003 --Consensus:IsLeader=true --Conduit:DirectoryUrl=ws://localhost:7000/");

            // 3. Start Node A (Follower) on 7002
            // Configured to redirect to 7003 (via Consensus LeaderUrl) but also register with Directory
            _nodeA = StartProcess(api2Path, "--Conduit:Port 7002 --Consensus:IsLeader=false --Consensus:LeaderUrl=ws://localhost:7003/ --Conduit:DirectoryUrl=ws://localhost:7000/");

            // Give them time to start up
            await Task.Delay(5000);

            try
            {
                // 4. Create Client connecting to Node A (Follower)
                var transport = new ConduitTransport();
                
                var filters = new List<IConduitFilter>
                {
                    new FixedTargetFilter("ws://localhost:7002/"),
                    new LeaderRoutingFilter()
                };
                var executor = new ConduitPipelineExecutor(transport, filters);

                var proxy = System.Reflection.DispatchProxy.Create<IUserService, ConduitProxy<IUserService>>();
                ((ConduitProxy<IUserService>)(object)proxy).Initialize(executor.ExecuteAsync);

                // 5. Call Service
                // This should be routed from Node A -> Node B (Leader)
                var newUser = new UserDto { Username = "telemetry_user", Email = "trace@example.com" };
                var created = await proxy.RegisterUserAsync(newUser);
                Assert.NotNull(created);

                var user = await proxy.GetUserAsync(created.Id);
                Assert.NotNull(user);
            }
            finally
            {
                Console.WriteLine("=== SERVER LOGS ===");
                Console.WriteLine(_logs.ToString());
                Console.WriteLine("===================");
            }
        }

        private class FixedTargetFilter : IConduitFilter
        {
            private readonly string _url;
            public FixedTargetFilter(string url) => _url = url;
            public ValueTask<ConduitMessage> InvokeAsync(ConduitMessage message, ConduitDelegate next)
            {
                message.Headers["Target-Url"] = _url;
                return next(message);
            }
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
