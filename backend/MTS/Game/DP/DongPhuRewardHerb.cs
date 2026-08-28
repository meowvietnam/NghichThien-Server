using MemoryPack;
using MTS.Models;

namespace MTS.Game.DP;
[MemoryPackable]
public partial class DongPhuRewardHerb : IDongPhuReward
{
    public ItemRef Herb { get; set; }
    public void Claim(CharacterData charData)
    {
        charData.InventoryManager.Add(Herb);
    }
    public string GetText()
    {
        var herbData = GameConfig.LuyenDanResources.FindHerb(Herb.Id);
        var quality = GameConfig.Core.Qualities.First(x => x.Tier == herbData.Tier);
        return $"x{Utils.Utils.FormatCount(Herb.Count)} <color={quality.Color.ToHexRGB()}>{herbData.Name}</color>";
    }
}