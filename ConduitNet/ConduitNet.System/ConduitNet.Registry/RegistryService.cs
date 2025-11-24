using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConduitNet.Contracts;

namespace ConduitNet.Registry
{
    public class RegistryService : IRegistryService
    {
        private readonly string _storageRoot;
        // In-memory index for now. In a real implementation, this would be the Raft State Machine.
        private readonly ConcurrentDictionary<string, List<PackageMetadata>> _packages = new();

        public RegistryService(string storageRoot = "./registry-data")
        {
            _storageRoot = storageRoot;
            if (!Directory.Exists(_storageRoot))
            {
                Directory.CreateDirectory(_storageRoot);
            }
        }

        public async Task<RegistryResult> PublishPackageAsync(PackageMetadata metadata, byte[] packageData)
        {
            try
            {
                // 1. Validate
                if (string.IsNullOrEmpty(metadata.Id) || string.IsNullOrEmpty(metadata.Version))
                {
                    return new RegistryResult { Success = false, Message = "Invalid package ID or Version." };
                }

                // 2. Check if version exists
                var versions = _packages.GetOrAdd(metadata.Id, _ => new List<PackageMetadata>());
                if (versions.Any(v => v.Version == metadata.Version))
                {
                    return new RegistryResult { Success = false, Message = $"Version {metadata.Version} already exists." };
                }

                // 3. Save to disk (Blob Storage)
                var packagePath = GetPackagePath(metadata.Id, metadata.Version);
                await File.WriteAllBytesAsync(packagePath, packageData);

                // 4. Update Index (State Machine)
                metadata.PublishedAt = DateTime.UtcNow;
                metadata.SizeBytes = packageData.Length;
                versions.Add(metadata);

                Console.WriteLine($"[Registry] Published {metadata.Id} v{metadata.Version}");

                return new RegistryResult 
                { 
                    Success = true, 
                    Message = "Published successfully.", 
                    PackageId = metadata.Id, 
                    Version = metadata.Version 
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Registry] Error publishing: {ex.Message}");
                return new RegistryResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<byte[]> GetPackageAsync(string packageName, string version)
        {
            var path = GetPackagePath(packageName, version);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Package {packageName} v{version} not found.");
            }

            return await File.ReadAllBytesAsync(path);
        }

        public Task<List<PackageVersion>> ListVersionsAsync(string packageName)
        {
            if (_packages.TryGetValue(packageName, out var versions))
            {
                return Task.FromResult(versions.Select(v => new PackageVersion 
                { 
                    Version = v.Version, 
                    PublishedAt = v.PublishedAt, 
                    IsDeprecated = false 
                }).ToList());
            }

            return Task.FromResult(new List<PackageVersion>());
        }

        public Task<PackageMetadata?> GetLatestVersionAsync(string packageName)
        {
            if (_packages.TryGetValue(packageName, out var versions) && versions.Any())
            {
                // Simple semantic versioning sort could be added here, for now taking the last added
                return Task.FromResult<PackageMetadata?>(versions.Last());
            }

            return Task.FromResult<PackageMetadata?>(null);
        }

        private string GetPackagePath(string id, string version)
        {
            var packageDir = Path.Combine(_storageRoot, id);
            if (!Directory.Exists(packageDir))
            {
                Directory.CreateDirectory(packageDir);
            }
            return Path.Combine(packageDir, $"{id}.{version}.cnp");
        }
    }
}
