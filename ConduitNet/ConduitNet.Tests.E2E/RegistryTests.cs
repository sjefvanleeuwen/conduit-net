using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ConduitNet.Client;
using ConduitNet.Contracts;
using ConduitNet.Core;
using System.Collections.Generic;
using System.Text;

namespace ConduitNet.Tests.E2E
{
    public class RegistryTests : IDisposable
    {
        private Process? _registryNode;
        private readonly StringBuilder _logs = new();
        private readonly string _storagePath;

        public RegistryTests()
        {
            _storagePath = Path.Combine(Path.GetTempPath(), "conduit_registry_test_" + Guid.NewGuid());
        }

        [Fact]
        public async Task Registry_Should_Accept_And_Serve_Packages()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var registryDll = Path.Combine(baseDir, "ConduitNet.Registry.dll");

            if (!File.Exists(registryDll)) throw new FileNotFoundException($"Registry DLL not found at {registryDll}");

            // 1. Start Registry Node on port 8000
            // We pass the storage root as an argument if the node supports it, or via env var/config.
            // The RegistryService currently defaults to "./registry-data". 
            // Let's assume we run it in a temp dir or just let it use the default relative path.
            // For better isolation, we should probably update RegistryService to take config, but for now let's run it.
            
            _registryNode = StartProcess(registryDll, "--Conduit:Port 8000");

            // Give it time to start
            await Task.Delay(3000);

            try
            {
                // 2. Create Client
                var transport = new ConduitTransport();
                var filters = new List<IConduitFilter>
                {
                    new FixedTargetFilter("ws://localhost:8000/")
                };
                var executor = new ConduitPipelineExecutor(transport, filters);
                var proxy = System.Reflection.DispatchProxy.Create<IRegistryService, ConduitProxy<IRegistryService>>();
                ((ConduitProxy<IRegistryService>)(object)proxy).Initialize(executor.ExecuteAsync);

                // 3. Publish a Package
                var packageData = Encoding.UTF8.GetBytes("Fake Zip Content");
                var metadata = new PackageMetadata
                {
                    Id = "com.test.package",
                    Version = "1.0.0",
                    Description = "Test Package"
                };

                var result = await proxy.PublishPackageAsync(metadata, packageData);
                
                Assert.True(result.Success, $"Publish failed: {result.Message}");
                Assert.Equal("com.test.package", result.PackageId);

                // 4. Retrieve the Package
                var downloadedData = await proxy.GetPackageAsync("com.test.package", "1.0.0");
                Assert.Equal(packageData, downloadedData);

                // 5. List Versions
                var versions = await proxy.ListVersionsAsync("com.test.package");
                Assert.Single(versions);
                Assert.Equal("1.0.0", versions[0].Version);
            }
            catch (Exception ex)
            {
                _logs.AppendLine($"TEST EXCEPTION: {ex}");
                throw;
            }
            finally
            {
                Console.WriteLine("=== REGISTRY LOGS ===");
                Console.WriteLine(_logs.ToString());
                Console.WriteLine("=====================");
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
            var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\" {args}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(dllPath) ?? string.Empty
            };
            var p = Process.Start(psi);
            
            p!.OutputDataReceived += (s, e) => { if (e.Data != null) lock(_logs) _logs.AppendLine($"[Registry] {e.Data}"); };
            p.ErrorDataReceived += (s, e) => { if (e.Data != null) lock(_logs) _logs.AppendLine($"[Registry ERROR] {e.Data}"); };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return p;
        }

        public void Dispose()
        {
            KillProcess(_registryNode);
            if (System.IO.Directory.Exists(_storagePath))
            {
                try { System.IO.Directory.Delete(_storagePath, true); } catch {}
            }
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
