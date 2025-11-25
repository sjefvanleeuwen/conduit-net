using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using ConduitNet.Client;
using ConduitNet.Contracts;
using ConduitNet.Core;
using System.Collections.Generic;

namespace ConduitNet.Tests.E2E
{
    public class AclServiceTests : IDisposable
    {
        private Process? _directory;
        private Process? _userService;
        private Process? _aclService;
        private readonly System.Text.StringBuilder _logs = new();

        [Fact]
        public async Task AclService_Should_Check_Permissions_Via_UserService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            var directoryPath = Path.Combine(baseDir, "ConduitNet.Directory.dll");
            var userServicePath = Path.Combine(baseDir, "ConduitNet.UserService.dll");
            var aclServicePath = Path.Combine(baseDir, "ConduitNet.AclService.dll");

            if (!File.Exists(directoryPath)) throw new FileNotFoundException($"Directory not found at {directoryPath}");
            if (!File.Exists(userServicePath)) throw new FileNotFoundException($"UserService not found at {userServicePath}");
            if (!File.Exists(aclServicePath)) throw new FileNotFoundException($"AclService not found at {aclServicePath}");

            // 1. Start Directory on 6000
            _directory = StartProcess(directoryPath, "--Conduit:Port 6000");

            // 2. Start User Service on 6002
            _userService = StartProcess(userServicePath, "--Conduit:Port 6002 --Conduit:DirectoryUrl=ws://localhost:6000/");

            // 3. Start ACL Service on 6003
            _aclService = StartProcess(aclServicePath, "--Conduit:Port 6003 --Conduit:DirectoryUrl=ws://localhost:6000/");

            // Give them time to start up and register
            await Task.Delay(8000);

            try
            {
                // 4. Create Client connecting to ACL Service directly (or via Directory, but let's go direct for simplicity of test setup)
                // We need to talk to User Service first to create a user
                var transport = new ConduitTransport();
                
                // Client for User Service (Direct to 6002)
                var userExecutor = new ConduitPipelineExecutor(transport, new List<IConduitFilter> { new FixedTargetFilter("ws://localhost:6002/") });
                var userProxy = System.Reflection.DispatchProxy.Create<IUserService, ConduitProxy<IUserService>>();
                ((ConduitProxy<IUserService>)(object)userProxy).Initialize(userExecutor.ExecuteAsync);

                // Client for ACL Service (Direct to 6003)
                var aclExecutor = new ConduitPipelineExecutor(transport, new List<IConduitFilter> { new FixedTargetFilter("ws://localhost:6003/") });
                var aclProxy = System.Reflection.DispatchProxy.Create<IAclService, ConduitProxy<IAclService>>();
                ((ConduitProxy<IAclService>)(object)aclProxy).Initialize(aclExecutor.ExecuteAsync);

                // 5. Create a User
                var newUser = new UserDto { Username = "acl_test_user", Email = "acl@example.com", Roles = new List<string> { "Admin" } };
                var createdUser = await userProxy.RegisterUserAsync(newUser);
                Assert.NotNull(createdUser);
                Assert.True(createdUser.Id > 0);

                // 6. Check Permission (Admin should have "CreateUser")
                // This call goes Client -> ACL Service -> User Service -> ACL Service (returns) -> Client
                var hasPermission = await aclProxy.CheckPermissionAsync(createdUser.Id, "CreateUser");
                
                Assert.True(hasPermission, "User with Admin role should have CreateUser permission");

                // 7. Check Negative Permission
                var hasFakePermission = await aclProxy.CheckPermissionAsync(createdUser.Id, "LaunchNukes");
                Assert.False(hasFakePermission, "User should not have random permission");
            }
            catch (Exception ex)
            {
                _logs.AppendLine($"TEST EXCEPTION: {ex}");
                throw;
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
            KillProcess(_directory);
            KillProcess(_userService);
            KillProcess(_aclService);
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
