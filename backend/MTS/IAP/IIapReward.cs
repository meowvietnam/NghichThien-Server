using MemoryPack;
using MTS.Models;

namespace MTS.IAP;
[MemoryPackable]
[MemoryPackUnion(0, typeof(StartPack))]
[MemoryPackUnion(1, typeof(MoneyVipPack))]
public partial interface IIapReward
{
    void Execute(CharacterData charData);
    void Refund(CharacterData charData);
}