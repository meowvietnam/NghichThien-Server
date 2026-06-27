using MemoryPack;
using MTS.Game;
using MTS.Game.Combat;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ResultCombatResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public RewardSnapshot Reward { get; set; } = new();
        public int NewHP { get; set; }
        public CamNgo[]? NewCamNgo { get; set; } = Array.Empty<CamNgo>();
        public int NewNgoDaoDiem { get; set; }
        public ETypeResultCombat TypeResultCombat { get; set; }

    }
}