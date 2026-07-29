using MemoryPack;
using MTS.Game.Rank;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncCharacterDataResult : Result
    {
        public CharacterWrapper Character { get; set; }
    }
}