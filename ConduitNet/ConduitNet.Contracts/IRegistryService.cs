using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConduitNet.Contracts
{
    public interface IRegistryService
    {
        /// <summary>
        /// Publishes a new package version to the registry.
        /// </summary>
        Task<RegistryResult> PublishPackageAsync(PackageMetadata metadata, byte[] packageData);

        /// <summary>
        /// Retrieves the binary content of a package.
        /// </summary>
        Task<byte[]> GetPackageAsync(string packageName, string version);

        /// <summary>
        /// Lists available versions for a specific package.
        /// </summary>
        Task<List<PackageVersion>> ListVersionsAsync(string packageName);

        /// <summary>
        /// Gets the latest version metadata for a package.
        /// </summary>
        Task<PackageMetadata?> GetLatestVersionAsync(string packageName);
    }
}
