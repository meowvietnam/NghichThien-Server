using MemoryPack;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.IAP;

[MemoryPackable]
public partial class StartPack : IIapReward
{
    public int MoneyVip { get; set; }
    public int Money { get; set; }
    public ushort CongPhapId { get; set; }

    public void Execute(CharacterData charData)
    {
        charData.SetIsInfiniteEnergy(true);
        charData.SetNoAds(true);
        charData.SetMoneyVip(charData.MoneyVip + MoneyVip);
        charData.Stats.SetLinhThach(charData.Stats.LinhThach + Money);
        var inventory = charData.InventoryManager;
        var congPhapRef = new ItemRef()
        {
            Type = ItemType.CongPhap,
            Id = CongPhapId,
            Count = 1,
        };
        inventory.Add(congPhapRef);
    }

    public void Refund(CharacterData charData)
    {
        charData.SetIsInfiniteEnergy(false);
        charData.SetNoAds(false);
        charData.SetMoneyVip(charData.MoneyVip - MoneyVip);
        charData.Stats.SetLinhThach(charData.Stats.LinhThach - Money);
        var inventory = charData.InventoryManager;
        var congPhapRef = new ItemRef()
        {
            Type = ItemType.CongPhap,
            Id = CongPhapId,
            Count = 1,
        };
        if (inventory.CongPhaps.Contains(congPhapRef))
        {
            inventory.Add(congPhapRef, -1);
        }
    }
    public void Edit(IapPurchase purchase)
    {
        purchase.MoneyVipSnapshot += MoneyVip;
    }
}