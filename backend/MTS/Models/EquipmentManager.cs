using System.ComponentModel.DataAnnotations.Schema;
using MTS.Data;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

public class EquipmentManager : IMemorySerializable<EquipmentManager>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }
    public HashSet<ThanThongEquip> ThanThongEquips { get; set; } = new();
    public HashSet<DanCombatUsed> DanCombatUseds { get; set; } = new();
    public ushort DonThuatUsed { get; set; }
    public ushort PhuTu1Used { get; set; }
    public ushort PhuTu2Used { get; set; }
    public ushort PhuTu3Used { get; set; }
    public ushort PhuTu4Used { get; set; }
    public ushort ChuTu1Used { get; set; }
    public ushort ChuTu2Used { get; set; }
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public CharacterData CharacterData { get; set; }

    public bool IsDefault()
    {
        return CharId == 0;
    }
    public void Deserialize(IReader reader, ref EquipmentManager value)
    {
        value.ThanThongEquips = reader.ReadArray<ThanThongEquip>().ToHashSet();
        value.DanCombatUseds = reader.ReadArray<DanCombatUsed>().ToHashSet();
        value.DonThuatUsed = reader.Read<ushort>();
        value.PhuTu1Used = reader.Read<ushort>();
        value.PhuTu2Used = reader.Read<ushort>();
        value.PhuTu3Used = reader.Read<ushort>();
        value.PhuTu4Used = reader.Read<ushort>();
        value.ChuTu1Used = reader.Read<ushort>();
        value.ChuTu2Used = reader.Read<ushort>();
    }

    public int ByteSize()
    {
        return ThanThongEquips.ByteSize() + DanCombatUseds.ByteSize() + DonThuatUsed.ByteSize() + PhuTu1Used.ByteSize() 
               + PhuTu2Used.ByteSize() + PhuTu3Used.ByteSize() + PhuTu4Used.ByteSize()
               + ChuTu1Used.ByteSize() + ChuTu2Used.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.WriteArray(ThanThongEquips);
        writer.WriteArray(DanCombatUseds);
        writer.Write(DonThuatUsed);
        writer.Write(PhuTu1Used);
        writer.Write(PhuTu2Used);
        writer.Write(PhuTu3Used);
        writer.Write(PhuTu4Used);
        writer.Write(ChuTu1Used);
        writer.Write(ChuTu2Used);
    }

    public void CopyFrom(EquipmentManager other)
    {
        ThanThongEquips = new(other.ThanThongEquips);
        DanCombatUseds = new(other.DanCombatUseds);
        DonThuatUsed = other.DonThuatUsed;
        PhuTu1Used = other.PhuTu1Used;
        PhuTu2Used = other.PhuTu2Used;
        PhuTu3Used = other.PhuTu3Used;
        PhuTu4Used = other.PhuTu4Used;
        ChuTu1Used = other.ChuTu1Used;
        ChuTu2Used = other.ChuTu2Used;
    }

    public async Task SaveChanges(AppDbContext db)
    {
    }
}