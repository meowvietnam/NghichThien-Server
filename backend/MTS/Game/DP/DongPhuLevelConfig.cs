using MemoryPack;
using MTS.Game.BaseSkill;
using MTS.Models;
using MTS.Utils;

namespace MTS.Game.DP;

[MemoryPackable]
public partial class DongPhuLevelConfig
{
    public string Name { get; set; }
    public DongPhuLevel Level { get; set; }
    public ushort SlotMaxWaterHelpPerDay { get; set; }
    public ushort MyMaxWaterHelpPerDay { get; set; }
    public TimeSpan ReduceTime { get; set; }
    public int RequireMoneyVip { get; set; }
    public Color Color { get; set; }
    public float Alpha { get; set; }

    [MemoryPackIgnore] public ItemRef RandomItem { get; set; }

    [MemoryPackIgnore] public ETierSkill[] TierSkill { get; set; }
    [MemoryPackIgnore] public int Amount { get; set; }
    public bool HasReward()
    {
        return RandomItem != null;
    }

    public Mail GetMailReward()
    {
        IEnumerable<BaseSkill.BaseSkill> skills = RandomItem.Type == ItemType.ThanThong
            ? GameConfig.NDCPTTManager.ThanThongs
            : GameConfig.NDCPTTManager.CongPhaps;
        var filterSkills = skills.Where(x => TierSkill.Contains(x.eTierSkill)).ToList();
        var rewards = new List<ItemRef>();
        for (int i = 0; i < RandomItem.Count; i++)
        {
            var skill = filterSkills.Random();
            var type = skill is ThanThong ? ItemType.ThanThong : ItemType.CongPhap;
            var itemRef = new ItemRef()
            {
                Id = (ushort)skill.Id,
                Count = 1,
                Type = type
            };
            rewards.Add(itemRef);
        }

        var mailRewards = rewards.GroupBy(x => x.Id)
            .Select(x => new ItemRef()
            {
                Id = x.Key,
                Count = (ushort)x.Sum(i => i.Count),
                Type = x.First().Type
            })
            .Select(x => new MailRewardItem()
            {
                Item = x
            } as IMailReward).ToList();
        var mail = new Mail()
        {
            Title = "Phần thưởng nâng cấp Động phủ",
            Content = $"Bạn đã nâng cấp thành công Động phủ {Name}, phần thưởng được đính kèm thư này",
            Rewards = mailRewards
        };
        return mail;
    }
}