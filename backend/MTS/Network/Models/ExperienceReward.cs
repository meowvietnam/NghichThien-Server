using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class ExperienceReward
{
    public int AddExp { get; set; }
    public int NewExp { get; set; }
}