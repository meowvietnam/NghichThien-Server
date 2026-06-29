using MemoryPack;
using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LoadCharCombatResult 
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public CharacterSnapshot Client { get; set; }
        public CharacterSnapshot Enemy { get; set; }

    }
}