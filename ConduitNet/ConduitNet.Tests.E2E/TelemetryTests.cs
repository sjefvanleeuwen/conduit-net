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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ConduitNet.Tests.E2E
{
    public class TelemetryTests : IDisposable
    {
        private Process? _directory;
        private Process? _telemetryNode;
        private Process? _userService;
        private readonly System.Text.StringBuilder _directoryLogs = new();
        private readonly System.Text.StringBuilder _telemetryLogs = new();
        private readonly System.Text.StringBuilder _userServiceLogs = new();

        [Fact]
        public async Task Telemetry_Should_Be_Collected_From_UserService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            var directoryPath = Path.Combine(baseDir, "ConduitNet.Directory.dll");
            var telemetryPath = Path.Combine(baseDir, "ConduitNet.Telemetry.Node.dll");
            var userServicePath = Path.Combine(baseDir, "ConduitNet.Examples.UserService.dll");

            if (!File.Exists(directoryPath)) throw new FileNotFoundException($"Directory not found at {directoryPath}");
            if (!File.Exists(telemetryPath)) throw new FileNotFoundException($"TelemetryNode not found at {telemetryPath}");
            if (!File.Exists(userServicePath)) throw new FileNotFoundException($"UserService not found at {userServicePath}");

            // 1. Start Directory on 6000
            _directory = StartProcess(directoryPath, "--Conduit:Port 6000", _directoryLogs);

            // 2. Start Telemetry Node on 6001, connect to Directory
            _telemetryNode = StartProcess(telemetryPath, "--Conduit:Port 6001 --Conduit:DirectoryUrl=ws://localhost:6000/ --Conduit:NodeUrl=ws://localhost:6001/", _telemetryLogs);

            // 3. Start User Service on 6002, connect to Directory
            _userService = StartProcess(userServicePath, "--Conduit:Port 6002 --Conduit:DirectoryUrl=ws://localhost:6000/ --Conduit:NodeUrl=ws://localhost:6002/", _userServiceLogs);

            // Give them time to start up and register
            await Task.Delay(5000);

            // 4. Make a request to UserService to trigger telemetry via Conduit RPC
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Conduit:DirectoryUrl"] = "ws://localhost:6000/"
                })
                .Build();
            
            services.AddSingleton<IConfiguration>(config);
            services.AddConduitClient<IConduitDirectory>();
            services.AddConduitClient<IUserService>();
            
            var sp = services.BuildServiceProvider();
            var userService = sp.GetRequiredService<IUserService>();

            try
            {
                var user = await userService.GetUserAsync(123);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UserService request failed: {ex.Message}");

                Console.WriteLine("=== DIRECTORY LOGS ===");
                Console.WriteLine(_directoryLogs.ToString());
                Console.WriteLine("=== TELEMETRY LOGS ===");
                Console.WriteLine(_telemetryLogs.ToString());
                Console.WriteLine("=== USER SERVICE LOGS ===");
                Console.WriteLine(_userServiceLogs.ToString());
                
                throw;
            }

            // 5. Wait for telemetry to be flushed (batch processor might have delay)
            // The SimpleActivityExportProcessor exports immediately, but there's network latency.
            // We'll poll the logs.
            
            var receivedTelemetry = false;
            for (int i = 0; i < 10; i++)
            {
                if (_telemetryLogs.ToString().Contains("[TelemetryCollector] Received batch"))
                {
                    receivedTelemetry = true;
                    break;
                }
                await Task.Delay(1000);
            }
            
            if (!receivedTelemetry)
            {
                Console.WriteLine("=== DIRECTORY LOGS ===");
                Console.WriteLine(_directoryLogs.ToString());
                Console.WriteLine("=== TELEMETRY LOGS (FAILURE) ===");
                Console.WriteLine(_telemetryLogs.ToString());
                Console.WriteLine("=== USER SERVICE LOGS ===");
                Console.WriteLine(_userServiceLogs.ToString());
            }

            Assert.True(receivedTelemetry, "Telemetry Node did not receive telemetry from UserService");
        }

        private Process StartProcess(string dllPath, string args, System.Text.StringBuilder logBuffer)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{dllPath} {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = psi };
            
            process.OutputDataReceived += (sender, e) => 
            {
                if (e.Data != null) logBuffer.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (sender, e) => 
            {
                if (e.Data != null) logBuffer.AppendLine(e.Data);
            };

            process.Start();
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        public void Dispose()
        {
            try { _userService?.Kill(); } catch {}
            try { _telemetryNode?.Kill(); } catch {}
            try { _directory?.Kill(); } catch {}
        }
    }
}
