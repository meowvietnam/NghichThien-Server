using MemoryPack;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class ItemMonsterRankReward : IMonsterRankReward
{
    public ItemRef Item { get; set; }
    public ETierSkill Tier { get; set; }
    public ItemType Type { get; set; }
    public ushort Count { get; set; }
    public void Claim(CharacterData charData)
    {
        if (Item == null)
        {
            Item = RandomSkill();
        }
        charData.InventoryManager.Add(Item);
    }
    private ItemRef RandomSkill()
    {
        IEnumerable<BaseSkill> skills = Type == ItemType.ThanThong
            ? GameConfig.NDCPTTManager.ThanThongs
            : GameConfig.NDCPTTManager.CongPhaps;
        var skill = skills.Where(x => x.eTierSkill == Tier).ToArray().Random();
        return new ItemRef()
        {
            Id = (ushort)skill.Id,
            Type = Type,
            Count = Count
        };
    }
}