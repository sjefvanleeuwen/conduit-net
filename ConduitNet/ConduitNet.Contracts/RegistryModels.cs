using System;
using System.Collections.Generic;
using MessagePack;

namespace ConduitNet.Contracts
{
    [MessagePackObject]
    public class PackageMetadata
    {
        [Key(0)]
        public string Id { get; set; } = string.Empty;
        [Key(1)]
        public string Version { get; set; } = string.Empty;
        [Key(2)]
        public string Type { get; set; } = "service"; // service, library, tool
        [Key(3)]
        public string Description { get; set; } = string.Empty;
        [Key(4)]
        public string EntryPoint { get; set; } = string.Empty;
        [Key(5)]
        public string Runtime { get; set; } = "dotnet-8.0";
        [Key(6)]
        public Dictionary<string, string> Dependencies { get; set; } = new();
        [Key(7)]
        public DateTime PublishedAt { get; set; }
        [Key(8)]
        public string Author { get; set; } = string.Empty;
        [Key(9)]
        public long SizeBytes { get; set; }
    }

    [MessagePackObject]
    public class RegistryResult
    {
        [Key(0)]
        public bool Success { get; set; }
        [Key(1)]
        public string Message { get; set; } = string.Empty;
        [Key(2)]
        public string PackageId { get; set; } = string.Empty;
        [Key(3)]
        public string Version { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class PackageVersion
    {
        [Key(0)]
        public string Version { get; set; } = string.Empty;
        [Key(1)]
        public DateTime PublishedAt { get; set; }
        [Key(2)]
        public bool IsDeprecated { get; set; }
    }
}
