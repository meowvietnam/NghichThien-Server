using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class LinhDanMonsterRankReward : IMonsterRankReward
{
    public ushort Id { get; set; }
    public ushort Count { get; set; }
    public void Claim(CharacterData charData)
    {
        var itemRef = new ItemRef()
        {
            CharId = charData.Id,
            Id = Id,
            Count = Count,
            Type = ItemType.LinhDan
        };
        charData.InventoryManager.Add(itemRef);
    }
}