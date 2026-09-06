using MemoryPack;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Utils;

namespace MTS.Models;
[MemoryPackable]
public partial class RandomSpecialSkillEventReward : IEventReward
{
    public List<ItemRef> Items { get; set; }
    public ETierSkill[] Tiers { get; set; }
    public ItemType Type { get; set; }
    public ETypeSkill TypeSkill { get; set; } = ETypeSkill.None;
    public ushort Count { get; set; }

    public void Claim(CharacterData charData)
    {
        Items = RandomSkill();
        foreach (var itemRef in Items)
        {
            charData.InventoryManager.Add(itemRef);
        }
    }

    public List<IMailReward> GetMailReward()
    {
        var items = RandomSkill();
        var mailRewards = items.Select(x => new MailRewardItem()
        {
            Item = x
        } as IMailReward).ToList();
        return mailRewards;
    }

    private List<ItemRef> RandomSkill()
    {
        IEnumerable<BaseSkill> skills = Type == ItemType.ThanThong
            ? GameConfig.NDCPTTManager.ThanThongs
            : GameConfig.NDCPTTManager.CongPhaps;
        var filterSkills = skills.Where(x =>
                Tiers.Contains(x.eTierSkill) && (TypeSkill == ETypeSkill.None || x.eTypeSkill.Contains(TypeSkill)))
            .ToList();
        var rewards = new List<ItemRef>();
        if (Type == ItemType.ThanThong)
        {
            for (int i = 0; i < Count; i++)
            {
                var skill = filterSkills.Random();

                var itemRef = new ItemRef()
                {
                    Id = (ushort)skill.Id,
                    Count = 1,
                    Type = Type
                };
                rewards.Add(itemRef);
            }
        }
        else
        {
            var countPerOne = 6;
            int totalRandomItem = Count / countPerOne;
            var remaining = Count % countPerOne;
            for (int i = 0; i < totalRandomItem; i++)
            {
                var skill = filterSkills.Random();
                var itemRef = new ItemRef()
                {
                    Id = (ushort)skill.Id,
                    Count = countPerOne,
                    Type = Type
                };
                rewards.Add(itemRef);
            }
            if (remaining > 0)
            {
                var skill = filterSkills.Random();
                var itemRef = new ItemRef()
                {
                    Id = (ushort)skill.Id,
                    Count = remaining,
                    Type = Type
                };
                rewards.Add(itemRef);
            }
        }

        rewards = rewards.GroupBy(x => x.Id)
            .Select(x => new ItemRef()
            {
                Id = x.Key,
                Count = x.Sum(i => i.Count),
                Type = x.First().Type
            }).ToList();
        return rewards;
    }
}