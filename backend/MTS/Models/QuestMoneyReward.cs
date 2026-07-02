using MemoryPack;
using MTS.Game.ItemProvider;

namespace MTS.Models;
[MemoryPackable]
public partial class QuestMoneyReward : IQuestReward
{
    public ItemRef Target { get; private set; }
    public int Money { get; private set; }
    [MemoryPackConstructor]
    public QuestMoneyReward()
    {
        
    }
    public QuestMoneyReward(ItemRef target)
    {
        Target = target;
        var type = target.Type;
        var id = target.Id;
        var count = target.Count;
        var provider = ItemProviderFactory.GetProvider(type, null);
        var price = provider.GetPriceItemSell(id);
        Money = price * count;
    }
    public void Claim(CharacterData charData)
    {
        var inventory = charData.InventoryManager;
        inventory.Decrease(Target);
        var stats = charData.Stats;
        var money = stats.LinhThach + Money;
        stats.SetLinhThach(money);
    }
}