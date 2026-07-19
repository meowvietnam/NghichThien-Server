using MemoryPack;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class SkillShop : Shop
{
    public override void Reload(ReloadShopRequest request)
    {
        var items = new HashSet<ItemRef>();
        var monPhai = request.MonPhai;
        var skill = MonPhaiToSkill(monPhai);
        var ndttcp = GameConfig.NDCPTTManager;
        var tierRates = new[]
        {
            new
            {
                Tier = ETierSkill.NhanGiaiHa,
                Rate = 50
            },
            new
            {
                Tier = ETierSkill.NhanGiaiTrung,
                Rate = 30
            },
            new
            {
                Tier = ETierSkill.NhanGiaiThuong,
                Rate = 20
            },
        };
        var tiers = tierRates.Select(x => x.Tier).ToArray();
        var count = Take();
        var congPhaps = ndttcp.CongPhaps
            .Where(x => (tiers.Contains(x.eTierSkill))
                        && QuerySkill(skill, x.eTypeSkill))
            .Select(x => new
            {
                Tier = x.eTierSkill,
                Ref = new ItemRef()
                {
                    Type = ItemType.CongPhap,
                    Id = (ushort)x.Id,
                    Count = RandomCount()
                }
            })
            .GroupBy(x => x.Tier)
            .ToDictionary(x => x.Key, x => x.Select(i => i.Ref).ToList());
        var randomCongPhap = new ItemRateRandomizer<List<ItemRef>>();
        foreach (var tierRate in tierRates)
        {
            if (congPhaps.TryGetValue(tierRate.Tier, out var list))
            {
                randomCongPhap.Add(list, tierRate.Rate);
            }
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                var list = randomCongPhap.Random();
                var item = list.Random();
                list.Remove(item);
                if (list.Count == 0)
                {
                    randomCongPhap.Remove(list);
                }

                items.Add(item);
            }
        }
        catch
        {
            //
        }
        
        var thanThongs = ndttcp.ThanThongs
            .Where(x => (tiers.Contains(x.eTierSkill))
                        && QuerySkill(skill, x.eTypeSkill))
            .Select(x => new
            {
                Tier = x.eTierSkill,
                Ref = new ItemRef()
                {
                    Type = ItemType.ThanThong,
                    Id = (ushort)x.Id,
                    Count = RandomCount()
                }
            })
            .GroupBy(x => x.Tier)
            .ToDictionary(x => x.Key, x => x.Select(i => i.Ref).ToList());
        var randomThanThong = new ItemRateRandomizer<List<ItemRef>>();
        foreach (var tierRate in tierRates)
        {
            if (thanThongs.TryGetValue(tierRate.Tier, out var list))
            {
                randomThanThong.Add(list, tierRate.Rate);
            }
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                var list = randomThanThong.Random();
                var item = list.Random();
                list.Remove(item);
                if (list.Count == 0)
                {
                    randomThanThong.Remove(list);
                }

                items.Add(item);
            }
        }
        catch
        {
            
        }
        Items = items;
        LastReloadTime = DateTime.UtcNow;
        Dirty = true;
    }

    private bool QuerySkill(ETypeSkill skill, ETypeSkill[] skills)
    {
        if (skill == ETypeSkill.None)
        {
            return true;
        }

        return skills.Contains(skill);
    }

    private ETypeSkill MonPhaiToSkill(EMonPhai monPhai)
    {
        switch (monPhai)
        {
            case EMonPhai.KimHongKiemPhai:
                return ETypeSkill.Kim;
            case EMonPhai.TrucSonTong:
                return ETypeSkill.Moc;
            case EMonPhai.TinhHaKiemPhai:
                return ETypeSkill.Thuy;
            case EMonPhai.LyHoaMon:
                return ETypeSkill.Hoa;
            case EMonPhai.HoaTranGiao:
                return ETypeSkill.Tho;
            case EMonPhai.BachDeLau:
                return ETypeSkill.Kiem;
            default:
                return ETypeSkill.None;
        }
    }

    protected override ushort RandomCount()
    {
        return 1;
    }

    public override EShopType GetTypeShop()
    {
        return EShopType.Skill;
    }
}