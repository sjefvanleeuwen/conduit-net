using MessagePack;

namespace ConduitNet.Contracts
{
    [MessagePackObject]
    public record UserDto
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string? Name { get; set; }
        [Key(2)] public string? Email { get; set; }
    }
}

