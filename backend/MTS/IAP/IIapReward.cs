using MemoryPack;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.IAP;
[MemoryPackable]
[MemoryPackUnion(0, typeof(StartPack))]
[MemoryPackUnion(1, typeof(MoneyVipPack))]
public partial interface IIapReward
{
    void Execute(CharacterData charData);
    void Refund(CharacterData charData);
    void Edit(IapPurchase purchase);
}