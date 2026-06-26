using MemoryPack;
using MTS.Models;

namespace MTS.IAP;
[MemoryPackable]
public partial class IapRewardTest : IIapReward
{
    public void Execute(CharacterData charData)
    {
    }
    public void Refund(CharacterData charData)
    {
    }
}
[MemoryPackable]
[MemoryPackUnion(100, typeof(IapRewardTest))]
public partial interface IIapReward
{
    void Execute(CharacterData charData);
    void Refund(CharacterData charData);
}