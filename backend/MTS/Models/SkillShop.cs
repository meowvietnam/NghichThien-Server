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
        var tiers = new[] { ETierSkill.NhanGiaiHa,ETierSkill.NhanGiaiTrung,ETierSkill.NhanGiaiThuong };
        var congPhaps = ndttcp.CongPhaps
            .Where(x => (tiers.Contains(x.eTierSkill))
                        && QuerySkill(skill, x.eTypeSkill))
            .OrderBy(x => Random.Shared.Next())
            .Take(Take())
            .Select(x => new ItemRef()
            {
                Type = ItemType.CongPhap,
                Id = (ushort)x.Id,
                Count = RandomCount()
            });
        items.AddRange(congPhaps);
        var thanThongs = ndttcp.ThanThongs
            .Where(x => (tiers.Contains(x.eTierSkill))
                        && QuerySkill(skill, x.eTypeSkill))
            .OrderBy(x => Random.Shared.Next())
            .Take(Take())
            .Select(x => new ItemRef()
            {
                Type = ItemType.ThanThong,
                Id = (ushort)x.Id,
                Count = RandomCount()
            });
        items.AddRange(thanThongs);
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