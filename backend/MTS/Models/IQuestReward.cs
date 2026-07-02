using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(QuestMoneyReward))]
[MemoryPackUnion(1, typeof(QuestMoneyVipReward))]
public partial interface IQuestReward
{
    public ItemRef Target { get; }
    void Claim(CharacterData charData);
}