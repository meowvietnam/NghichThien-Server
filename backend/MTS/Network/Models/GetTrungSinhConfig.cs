using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class GetTrungSinhConfig : Result
{
    public TrungSinhConfig Config { get; set; }
}