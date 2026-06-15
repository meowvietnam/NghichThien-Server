using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Game.LuyenKhi;
using MTS.Network.Models;
using MTS.Utils;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

[MemoryPackable]
public partial class Equipment : IEquatable<Equipment>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    public ushort SelfId { get; set; }
    public ushort Id { get; set; }
    public string Name { get; set; }
    public Effect[] Effects { get; set; }
    public ushort[] MaterialIds { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public InventoryManager Inventory { get; set; }

    [NotMapped]
    [MemoryPackIgnore]
    public LuyenKhiRewardItem Item => LuyenKhiResources.Instance.RewardItems.FirstOrDefault(x => x.Id == Id);

    [NotMapped]
    [MemoryPackIgnore]
    public Material[] Materials =>
        LuyenKhiResources.Instance.Materials.Where(x => MaterialIds.Contains((ushort)x.Id)).ToArray();

    [NotMapped]
    [MemoryPackIgnore]
    public EquipmentGradeConfig GradeConfig =>
        LuyenKhiResources.Instance.EquipmentGrades.FirstOrDefault(x => x.Grade == GetGrade());

    public EquipmentGrade GetGrade()
    {
        var materials = Materials;
        var res = LuyenKhiResources.Instance;
        var matConfigs = res.MaterialGradeConfigs;
        var grade = 0;
        foreach (var material in materials)
        {
            var matConfig = matConfigs.First(x => x.Tier == material.Tier);
            grade += matConfig.Grade;
        }

        var equipmentGradeConfig = res.EquipmentGrades.Last(x => x.MinGrade <= grade);
        return equipmentGradeConfig.Grade;
    }

    public EquipmentType GetEquipmentType()
    {
        var artifactType = Item.Type;
        switch (artifactType)
        {
            case ArtifactType.PhiKiem:
            case ArtifactType.BaoChung:
            case ArtifactType.ChiHoan:
            case ArtifactType.PhiCham:
            case ArtifactType.BaoHap:
                return EquipmentType.Weapon;
            case ArtifactType.TruongBao:
            case ArtifactType.GiapTru:
                return EquipmentType.Armor;
            default:
                return EquipmentType.Jewelry;
        }
    }

    public bool Equals(Equipment? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SelfId == other.SelfId && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SelfId, Id);
    }

    public Material[] GetMaterialsWithEffectType(EffectType effectType)
    {
        var artifactType = Item.Type;
        var config = LuyenKhiResources.Instance.LianQiEffects.FirstOrDefault(c =>
            c.Effects.Any(e => e.EffectType == effectType && e.ArtifactType == artifactType)
        );
        if (config == null) return Array.Empty<Material>();
        return Materials.Where(m =>
            m.CultivationElement == config.CultivationElement &&
            m.YinYangType == config.YinYangType
        ).ToArray();
    }

    public (int min, int max) GetMinMaxValue(EffectType effectType)
    {
        var artifactType = Item.Type;
        var allConfigs = LuyenKhiResources.Instance.LianQiEffects;

        var validConfigs = allConfigs.Where(c =>
            c.Effects.Any(e => e.EffectType == effectType && e.ArtifactType == artifactType)
        ).ToList();
        if (validConfigs.Count == 0) return (0, 0);
        var effectData = validConfigs[0].Effects
            .First(e => e.EffectType == effectType && e.ArtifactType == artifactType);
        if (effectData.Value == 0) return (0, 0);
        int sumGrade = 0;
        foreach (var config in validConfigs)
        {
            sumGrade += Materials
                .Where(m => m.CultivationElement == config.CultivationElement
                            && m.YinYangType == config.YinYangType)
                .Sum(m => LuyenKhiResources.Instance.MaterialGradeConfigs
                    .FirstOrDefault(x => x.Tier == m.Tier)?.Grade ?? 0);
        }

        int maxValue = Mathf.RoundToInt(1.0f * sumGrade / effectData.Value);
        int minValue = Mathf.RoundToInt(0.5f * maxValue);
        return (minValue, maxValue);
    }
}