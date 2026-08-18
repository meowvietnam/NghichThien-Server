using MemoryPack;
using MTS.Data;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Models;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.IAP;

[MemoryPackable]
public partial class MonthlyPassPack : IIapReward
{
    public int MoneyVipPerDay { get; set; }

    public void Edit(IapPurchase purchase)
    {
    }

    public List<IMailReward> GetMailRewards()
    {
        var items = RandomItem(1);
        var rewards = GetMailRewards(items);
        rewards.Add(new MailMonthlyPass()
        {
            MoneyVipPerDay = MoneyVipPerDay,
            PassName = "Thẻ tháng thường"
        });
        return rewards;
    }

    public static List<ItemRef> RandomItem(ushort count)
    {
        var tier1 = new[] { ETierSkill.DiaGiaiHa, ETierSkill.DiaGiaiTrung, ETierSkill.DiaGiaiThuong };
        var tier2 = new[] { ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong };
        var thanThongs = GameConfig.NDCPTTManager.ThanThongs;
        var congPhaps = GameConfig.NDCPTTManager.CongPhaps;
        var tt1 = thanThongs.Where(j => tier1.Contains(j.eTierSkill)).ToArray();
        var tt2 = thanThongs.Where(j => tier2.Contains(j.eTierSkill)).ToArray();
        var cp1 = congPhaps.Where(j => tier1.Contains(j.eTierSkill)).ToArray();
        var cp2 = congPhaps.Where(j => tier2.Contains(j.eTierSkill)).ToArray();
        var listItems = new List<ItemRef>();
        for (int i = 0; i < count; i++)
        {
            var t1 = tt1.Random().ToItemRef(1);
            var t2 = tt2.ToArray().Random().ToItemRef(1);
            listItems.Add(t1);
            listItems.Add(t2);
        }
        var c1 = cp1.Random().ToItemRef(count);
        var c2 = cp2.Random().ToItemRef(count);
        listItems.Add(c1);
        listItems.Add(c2);
        return listItems
            .GroupBy(x => new { x.Id, x.Type })
            .Select(g => new ItemRef
            {
                Id = g.Key.Id,
                Type = g.Key.Type,
                Count = (ushort)g.Sum(x => x.Count)
            })
            .ToList();
    }

    public static List<IMailReward> GetMailRewards(List<ItemRef> items)
    {
        var rewards = new List<IMailReward>();
        foreach (var itemRef in items)
        {
            var type = itemRef.Type;
            if (type == ItemType.Furnace)
            {
                rewards.Add(new MailRewardFurnace()
                {
                    FurnaceData = new FurnaceData()
                    {
                        Id = itemRef.Id,
                    }
                });
            }
            else
            {
                rewards.Add(new MailRewardItem()
                {
                    Item = itemRef,
                });
            }
        }

        return rewards;
    }
}