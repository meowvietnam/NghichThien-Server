using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MoneyVipEventReward))]
[MemoryPackUnion(1, typeof(RandomSkillEventReward))]
[MemoryPackUnion(2, typeof(LinhDanEventReward))]
[MemoryPackUnion(3, typeof(ItemEventReward))]
[MemoryPackUnion(4, typeof(RandomSpecialSkillEventReward))]
[MemoryPackUnion(5, typeof(SkillEventReward))]
public partial interface IEventReward
{
    void Claim(CharacterData charData);
    List<IMailReward> GetMailReward();
}