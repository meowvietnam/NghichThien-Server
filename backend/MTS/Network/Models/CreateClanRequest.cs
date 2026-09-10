using MemoryPack;

namespace MTS.Network.Models;

public enum CreateClanBy
{
    MoneyVip,
    Vnd
}

[MemoryPackable]
public partial class CreateClanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Announcement { get; set; } = string.Empty;
    public int NodeId { get; set; }
    public string? ImageUrl { get; set; }
    public CreateClanBy CreateBy { get; set; }
    [MemoryPackIgnore]
    public int LeaderCharId { get; set; }
    [MemoryPackIgnore]
    public int MaxMembers { get; set; }
}
