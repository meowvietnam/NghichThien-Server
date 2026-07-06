using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class ClaimRankRewardResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public long Id { get; set; }
    public int MoneyVip { get; set; }
}