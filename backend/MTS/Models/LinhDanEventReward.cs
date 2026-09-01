using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class LinhDanEventReward : IEventReward
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

    public List<IMailReward> GetMailReward()
    {
        return new List<IMailReward>()
        {
            new MailRewardItem()
            {
                Item = new ItemRef()
                {
                    Id = Id,
                    Count = Count,
                    Type = ItemType.LinhDan
                }
            }
        };
    }
}