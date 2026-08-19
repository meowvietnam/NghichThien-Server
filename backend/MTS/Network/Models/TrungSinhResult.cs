using MemoryPack;
using MTS.Game.Rank;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class TrungSinhResult : Result
{
    public CharacterWrapper Wrapper { get; set; }
}