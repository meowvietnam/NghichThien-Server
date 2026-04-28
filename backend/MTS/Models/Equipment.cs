using System.ComponentModel.DataAnnotations.Schema;
using MTS.Game.LuyenKhi;
using MTS.Network.Models;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

public class Equipment : IMemorySerializable<Equipment> , IEquatable<Equipment>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }
    public ushort SelfId { get; set; }
    public ushort Id { get; set; }
    public string Name { get; set; }
    public Effect[] Effects { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public InventoryManager Inventory { get; set; }
    [NotMapped] public LuyenKhiRewardItem Item => LuyenKhiResources.Instance.RewardItems.FirstOrDefault(x => x.Id == Id);
    public void Deserialize(IReader reader, ref Equipment value)
    {
        value.SelfId = reader.Read<ushort>();
        value.Id = reader.Read<ushort>();
        value.Name = reader.Read();
        value.Effects = reader.ReadArray<Effect>();
    }
    public int ByteSize()
    {
        return SelfId.ByteSize() + Id.ByteSize() + Name.ByteSize() + Effects.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(SelfId);
        writer.Write(Id);
        writer.Write(Name);
        writer.WriteArray(Effects);
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
}