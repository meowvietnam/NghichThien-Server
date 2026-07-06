using MemoryPack;
using MTS.Models;

namespace MTS.Game.Rank;
[MemoryPackable]
public partial class MoneyVipSpendingRankEntry
{
    public CharacterWrapper CharacterWrapper { get; private set; }
    public long TotalSpent { get; private set; }
    [MemoryPackConstructor]
    public MoneyVipSpendingRankEntry()
    {
        
    }
    public MoneyVipSpendingRankEntry(CharacterData characterData, long totalSpent)
    {
        CharacterWrapper = new CharacterWrapper(characterData);
        TotalSpent = totalSpent;
    }
}