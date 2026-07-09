using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Utils;
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
    public ushort DonThuatUsed { get; set; } = ushort.MaxValue;
    public ushort PhuTu1Used { get; set; } = ushort.MaxValue;
    public ushort PhuTu2Used { get; set; } = ushort.MaxValue;
    public ushort PhuTu3Used { get; set; } = ushort.MaxValue;
    public ushort PhuTu4Used { get; set; } = ushort.MaxValue;
    public ushort ChuTu1Used { get; set; } = ushort.MaxValue;
    public ushort ChuTu2Used { get; set; } = ushort.MaxValue;
    public ushort VuKhi { get; set; } = ushort.MaxValue;
    public ushort PhongCu { get; set; } = ushort.MaxValue;
    public ushort TrangSuc { get; set; } = ushort.MaxValue;
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public CharacterData CharacterData { get; set; }

    [NotMapped] private EquipmentTracking tracking = new();
    [NotMapped] private bool isDirty1;
    [NotMapped] private bool isDirty2;
    // Hoặc nếu muốn viết tường minh, dễ gọi, tách làm 3 hàm như sau cho gọn:
    public void EquipVuKhi(ushort id)
    {
        VuKhi = id;
        isDirty2 = true;
    }

    public void EquipPhongCu(ushort id)
    {
        PhongCu = id;
        isDirty2 = true;
    }

    public void EquipTrangSuc(ushort id)
    {
        TrangSuc = id;
        isDirty2 = true;
    }
    public void ClearDanCombat()
    {
        foreach (var danCombatUsed in DanCombatUseds)
        {
            tracking.DanCombatUseds.Delete(danCombatUsed);
        }
        DanCombatUseds.Clear();
        isDirty1 = true;
    }
    public ThanThongEquip EquipThanThong(EquipThanThong request)
    {
        var thanThong = new ThanThongEquip()
        {
            Id = request.Id,
            CharId = CharId,
            SlotIndex = request.Index
        };
        var oldSlot = ThanThongEquips.FirstOrDefault(x => x.SlotIndex == thanThong.SlotIndex);
        if (oldSlot != null)
        {
            ThanThongEquips.Remove(oldSlot);
            tracking.ThanThongEquips.Delete(thanThong);
            isDirty1 = true;
        }
        var add = ThanThongEquips.Add(thanThong);
        if (add)
        {
            tracking.ThanThongEquips.Add(thanThong);
            isDirty1 = true;
            return thanThong;
        }

        return null;
    }

    public bool UnequipThanThong(ushort id)
    {
        var thanThong = new ThanThongEquip()
        {
            Id = id,
            CharId = CharId
        };
        var remove = ThanThongEquips.Remove(thanThong);
        if (remove)
        {
            tracking.ThanThongEquips.Delete(thanThong);
            isDirty1 = true;
        }
        return remove;
    }

    public ushort GetSlotCP(ETypeSlotCP slot)
    {
        switch (slot)
        {
            case ETypeSlotCP.ChuTu1:
                return ChuTu1Used;
            case ETypeSlotCP.ChuTu2:
                return ChuTu2Used;
            case ETypeSlotCP.DonThuat:
                return DonThuatUsed;
            case ETypeSlotCP.PhuTu1:
                return PhuTu1Used;
            case ETypeSlotCP.PhuTu2:
                return PhuTu2Used;
            case ETypeSlotCP.PhuTu3:
                return PhuTu3Used;
            case ETypeSlotCP.PhuTu4:
                return PhuTu4Used;
            default:
                throw new Exception(Strings.SlotCongPhapInvalid);
        }
    }

    public void SetSlotCP(ETypeSlotCP slot, ushort value)
    {
        isDirty2 = true;
        switch (slot)
        {
            case ETypeSlotCP.ChuTu1:
                ChuTu1Used = value;
                break;
            case ETypeSlotCP.ChuTu2:
                var level = CharacterData.Progression.Level;
                if (level < 10)
                {
                    throw new Exception(Strings.SlotCongPhapLevelRequired);
                }
                ChuTu2Used = value;
                break;
            case ETypeSlotCP.DonThuat:
                DonThuatUsed = value;
                break;
            case ETypeSlotCP.PhuTu1:
                PhuTu1Used = value;
                break;
            case ETypeSlotCP.PhuTu2:
                PhuTu2Used = value;
                break;
            case ETypeSlotCP.PhuTu3:
                PhuTu3Used = value;
                break;
            case ETypeSlotCP.PhuTu4:
                PhuTu4Used = value;
                break;
            default:
                isDirty2 = false;
                throw new Exception(Strings.SlotCongPhapInvalid);
        }
    }
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
        value.VuKhi = reader.Read<ushort>();
        value.PhongCu = reader.Read<ushort>();
        value.TrangSuc = reader.Read<ushort>();
    }

    public int ByteSize()
    {
        return ThanThongEquips.ByteSize() + DanCombatUseds.ByteSize() + DonThuatUsed.ByteSize() +
               PhuTu1Used.ByteSize()
               + PhuTu2Used.ByteSize() + PhuTu3Used.ByteSize() + PhuTu4Used.ByteSize()
               + ChuTu1Used.ByteSize() + ChuTu2Used.ByteSize()
               + VuKhi.ByteSize() + PhongCu.ByteSize() + TrangSuc.ByteSize();
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
        writer.Write(VuKhi);
        writer.Write(PhongCu);
        writer.Write(TrangSuc);
    }

    public void CopyFrom(EquipmentManager other)
    {
        ThanThongEquips = new(other.ThanThongEquips);
        DanCombatUseds = new(other.DanCombatUseds);
        DonThuatUsed = other.DonThuatUsed;
        ChuTu1Used = other.ChuTu1Used;
        ChuTu2Used = other.ChuTu2Used;
        PhuTu1Used = other.PhuTu1Used;
        PhuTu2Used = other.PhuTu2Used;
        PhuTu3Used = other.PhuTu3Used;
        PhuTu4Used = other.PhuTu4Used;
        VuKhi = other.VuKhi;
        PhongCu = other.PhongCu;
        TrangSuc = other.TrangSuc;
    }

    public void Update(DanCombatUsed danCombatUsed)
    {
        tracking.DanCombatUseds.Update(danCombatUsed);
    }

    public void Add(DanCombatUsed danCombatUsed)
    {
        danCombatUsed.CharId = CharId;
        DanCombatUseds.Add(danCombatUsed);
        tracking.DanCombatUseds.Add(danCombatUsed);
    }

    public void Remove(DanCombatUsed danCombatUsed)
    {
        danCombatUsed.CharId = CharId;
        DanCombatUseds.Remove(danCombatUsed);
        tracking.DanCombatUseds.Delete(danCombatUsed);
    }
    public async Task SaveChanges(AppDbContext db)
    {
        if (isDirty1)
        {
            tracking.Apply(db);
            tracking.Clear();
            isDirty1 = false; // Nên reset cờ sau khi lưu thành công babe nhé
        }

        if (isDirty2)
        {
            await db.EquipmentManagers.Where(x => x.CharId == CharId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.DonThuatUsed, DonThuatUsed)
                    .SetProperty(x => x.ChuTu1Used, ChuTu1Used)
                    .SetProperty(x => x.ChuTu2Used, ChuTu2Used)
                    .SetProperty(x => x.PhuTu1Used, PhuTu1Used)
                    .SetProperty(x => x.PhuTu2Used, PhuTu2Used)
                    .SetProperty(x => x.PhuTu3Used, PhuTu3Used)
                    .SetProperty(x => x.PhuTu4Used, PhuTu4Used)
            // --- BỔ SUNG 3 DÒNG LƯU TRANG BỊ MỚI LÊN DATABASE TẠI ĐÂY ---
                    .SetProperty(x => x.VuKhi, VuKhi)
                    .SetProperty(x => x.PhongCu, PhongCu)
                    .SetProperty(x => x.TrangSuc, TrangSuc));
            isDirty2 = false; // Reset cờ báo thay đổi về false để tránh lưu trùng lặp lần sau
        }
    }

}
