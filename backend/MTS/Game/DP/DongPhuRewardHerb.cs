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
        return $"x{Utils.Utils.FormatCount(Herb.Count)} {herbData.Name}";
    }
}