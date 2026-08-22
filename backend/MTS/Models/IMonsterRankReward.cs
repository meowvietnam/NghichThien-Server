using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MoneyVipMonsterRankReward))]
[MemoryPackUnion(1, typeof(ItemMonsterRankReward))]
public partial interface IMonsterRankReward
{
    void Claim(CharacterData charData);
}