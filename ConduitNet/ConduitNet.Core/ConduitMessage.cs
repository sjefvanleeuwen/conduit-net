using MessagePack;
using System;
using System.Collections.Generic;

namespace ConduitNet.Core
{
    [MessagePackObject]
    public class ConduitMessage
    {
        [Key(0)] public string Id { get; set; } = Guid.NewGuid().ToString();
        [Key(1)] public string MethodName { get; set; } = string.Empty;
        [Key(2)] public byte[] Payload { get; set; } = Array.Empty<byte>();
        [Key(3)] public bool IsError { get; set; }
        [Key(4)] public Dictionary<string, string> Headers { get; set; } = new();
        [Key(5)] public string InterfaceName { get; set; } = string.Empty;
    }
}

