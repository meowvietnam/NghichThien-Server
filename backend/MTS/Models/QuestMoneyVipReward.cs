using MemoryPack;
using MTS.Game;
using MTS.Game.ItemProvider;
using MTS.Game.LuyenDan;

namespace MTS.Models;

[MemoryPackable]
public partial class QuestMoneyVipReward : IQuestReward
{
    public int Money { get; private set; }
    public int MoneyVip { get; private set; }
    public ItemRef Target { get; private set; }
    public QuestMoneyVipReward(ItemRef target)
    {
        Target = target;
        var type = target.Type;
        var id = target.Id;
        var count = target.Count;
        var provider = ItemProviderFactory.GetProvider(type, null);
        var price = provider.GetPriceItemSell(id);
        Money = price * count;
        IItemData item = null!;
        if (type == ItemType.Herb)
        {
            item = GameConfig.LuyenDanResources.FindHerb(id);
        }
        else if (type == ItemType.Material)
        {
            item = GameConfig.LuyenKhiResources.FindMaterial(id);
        }
        MoneyVip = GameConfig.PriceTienNgocVatLieuAndHerbTask[item.Tier] * count;
    }
    public void Claim(CharacterData charData)
    {
        var inventory = charData.InventoryManager;
        inventory.Decrease(Target);
        var stats = charData.Stats;
        var money = stats.LinhThach + Money;
        stats.SetLinhThach(money);
        var moneyVip = charData.MoneyVip + MoneyVip;
        charData.SetMoneyVip(moneyVip);
    }
}