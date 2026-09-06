using MemoryPack;

namespace MTS.Models
{
    [MemoryPackable]
    public partial class SkillEventReward : IEventReward
    {
        public ItemRef Item { get; set; }

        public void Claim(CharacterData charData)
        {
            charData.InventoryManager.Add(Item);
        }

        public List<IMailReward> GetMailReward()
        {
            return new List<IMailReward>()
            {
                new MailRewardItem()
                {
                    Item = Item
                }
            };
        }
    }
}