using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game.BaseSkill;
using MTS.Game.Gacha;

namespace MTS.Models;

[MemoryPackable]
public partial class GachaManager
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public int GachaThanThong { get; set; }
    public int GachaCongPhap { get; set; }
    public int GachaDanDuoc { get; set; }
    public int GachaDuocLieu { get; set; }
    public int GachaVatLieu { get; set; }
    public Dictionary<ETypeSkill, int> GachaThanThongDict { get; set; } = new();
    public Dictionary<ETypeSkill, int> GachaCongPhapDict { get; set; } = new();
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [NotMapped]
    public int Threshold { get; set; } = 50;

    [ForeignKey(nameof(CharId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public CharacterData CharData { get; set; }

    [NotMapped] [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore]
    private bool dirty = false;

    public async Task SaveChanges(AppDbContext db)
    {
        if (!dirty) return;
        await db.GachaManagers
            .Where(x => x.CharId == CharId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.GachaThanThongDict, GachaThanThongDict)
                    .SetProperty(x => x.GachaCongPhapDict, GachaCongPhapDict)
                    .SetProperty(x => x.GachaDanDuoc, GachaDanDuoc)
                    .SetProperty(x => x.GachaDuocLieu, GachaDuocLieu)
                    .SetProperty(x => x.GachaVatLieu, GachaVatLieu));
        dirty = false;
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public void Set(ETypeGacha type,ETypeSkill skill, int value)
    {
        var oldValue = Get(type,skill);
        if (oldValue == value)
        {
            return;
        }
        switch (type)
        {
            case ETypeGacha.ThanThong:
                GachaThanThongDict[skill] = value;
                break;
            case ETypeGacha.CongPhap:
                GachaCongPhapDict[skill] = value;
                break;
            case ETypeGacha.DanDuoc:
                GachaDanDuoc = value;
                break;
            case ETypeGacha.DuocLieu:
                GachaDuocLieu = value;
                break;
            case ETypeGacha.VatLieu:
                GachaVatLieu = value;
                break;
        }
        dirty = true;
    }
    public int Get(ETypeGacha type, ETypeSkill skill)
    {
        switch (type)
        {
            case ETypeGacha.ThanThong:
                if (GachaThanThongDict.TryGetValue(skill, out var ttValue))
                {
                    return ttValue;
                }
                return 0;
            case ETypeGacha.CongPhap:
                if (GachaCongPhapDict.TryGetValue(skill, out var cpValue))
                {
                    return cpValue;
                }
                return 0;
            case ETypeGacha.DanDuoc:
                return GachaDanDuoc;
            case ETypeGacha.DuocLieu:
                return GachaDuocLieu;
            case ETypeGacha.VatLieu:
                return GachaVatLieu;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}