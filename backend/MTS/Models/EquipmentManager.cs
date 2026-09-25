using MTS.Exceptions;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Utils;
using MemoryPack;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

[MemoryPackable]
public partial class EquipmentManager
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
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
    [MemoryPackIgnore]
    public CharacterData CharacterData { get; set; }

    [NotMapped]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    private readonly object equipmentLock = new();

    [NotMapped]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public object SyncRoot => equipmentLock;

    [NotMapped] private EquipmentTracking tracking = new();
    [NotMapped] private bool isDirty1;
    [NotMapped] private bool isDirty2;

    public ushort[] GetAllSlotsCP()
    {
        lock (equipmentLock)
        {
            return new[] { DonThuatUsed, PhuTu1Used, PhuTu2Used, PhuTu3Used, PhuTu4Used, ChuTu1Used, ChuTu2Used };
        }
    }
    public void EquipVuKhi(ushort id)
    {
        lock (equipmentLock)
        {
            VuKhi = id;
            isDirty2 = true;
        }
    }

    public void EquipPhongCu(ushort id)
    {
        lock (equipmentLock)
        {
            PhongCu = id;
            isDirty2 = true;
        }
    }

    public void EquipTrangSuc(ushort id)
    {
        lock (equipmentLock)
        {
            TrangSuc = id;
            isDirty2 = true;
        }
    }
    public void ClearDanCombat()
    {
        lock (equipmentLock)
        {
            foreach (var danCombatUsed in DanCombatUseds)
            {
                tracking.DanCombatUseds.Delete(danCombatUsed);
            }

            DanCombatUseds.Clear();
            isDirty1 = true;
        }
    }
    public ThanThongEquip EquipThanThong(EquipThanThong request)
    {
        lock (equipmentLock)
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
                tracking.ThanThongEquips.Delete(oldSlot);
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
    }

    public bool UnequipThanThong(ushort id)
    {
        lock (equipmentLock)
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
    }

    public ushort GetSlotCP(ETypeSlotCP slot)
    {
        lock (equipmentLock)
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
                    throw new InvalidException(Strings.SlotCongPhapInvalid);
            }
        }
    }

    public void SetSlotCP(ETypeSlotCP slot, ushort value)
    {
        lock (equipmentLock)
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
                        throw new InvalidException(Strings.SlotCongPhapLevelRequired);
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
                    throw new InvalidException(Strings.SlotCongPhapInvalid);
            }
        }
    }
    public bool IsDefault()
    {
        return CharId == 0;
    }
    public void CopyFrom(EquipmentManager other)
    {
        HashSet<ThanThongEquip> thanThongEquips;
        HashSet<DanCombatUsed> danCombatUseds;
        ushort donThuatUsed;
        ushort chuTu1Used;
        ushort chuTu2Used;
        ushort phuTu1Used;
        ushort phuTu2Used;
        ushort phuTu3Used;
        ushort phuTu4Used;
        ushort vuKhi;
        ushort phongCu;
        ushort trangSuc;

        lock (other.SyncRoot)
        {
            thanThongEquips = new(other.ThanThongEquips);
            danCombatUseds = new(other.DanCombatUseds);
            donThuatUsed = other.DonThuatUsed;
            chuTu1Used = other.ChuTu1Used;
            chuTu2Used = other.ChuTu2Used;
            phuTu1Used = other.PhuTu1Used;
            phuTu2Used = other.PhuTu2Used;
            phuTu3Used = other.PhuTu3Used;
            phuTu4Used = other.PhuTu4Used;
            vuKhi = other.VuKhi;
            phongCu = other.PhongCu;
            trangSuc = other.TrangSuc;
        }

        lock (equipmentLock)
        {
            ThanThongEquips = thanThongEquips;
            DanCombatUseds = danCombatUseds;
            DonThuatUsed = donThuatUsed;
            ChuTu1Used = chuTu1Used;
            ChuTu2Used = chuTu2Used;
            PhuTu1Used = phuTu1Used;
            PhuTu2Used = phuTu2Used;
            PhuTu3Used = phuTu3Used;
            PhuTu4Used = phuTu4Used;
            VuKhi = vuKhi;
            PhongCu = phongCu;
            TrangSuc = trangSuc;
        }
    }

    public void Update(DanCombatUsed danCombatUsed)
    {
        lock (equipmentLock)
        {
            tracking.DanCombatUseds.Update(danCombatUsed);
            isDirty1 = true;
        }
    }

    public void Add(DanCombatUsed danCombatUsed)
    {
        lock (equipmentLock)
        {
            danCombatUsed.CharId = CharId;
            DanCombatUseds.Add(danCombatUsed);
            tracking.DanCombatUseds.Add(danCombatUsed);
            isDirty1 = true;
        }
    }

    public void Remove(DanCombatUsed danCombatUsed)
    {
        lock (equipmentLock)
        {
            danCombatUsed.CharId = CharId;
            DanCombatUseds.Remove(danCombatUsed);
            tracking.DanCombatUseds.Delete(danCombatUsed);
            isDirty1 = true;
        }
    }
    public async Task SaveChanges(AppDbContext db)
    {
        ushort donThuatUsed;
        ushort chuTu1Used;
        ushort chuTu2Used;
        ushort phuTu1Used;
        ushort phuTu2Used;
        ushort phuTu3Used;
        ushort phuTu4Used;
        ushort vuKhi;
        ushort phongCu;
        ushort trangSuc;
        bool shouldSaveSlots;

        lock (equipmentLock)
        {
            if (isDirty1)
            {
                tracking.Apply(db);
            }

            shouldSaveSlots = isDirty2;
            donThuatUsed = DonThuatUsed;
            chuTu1Used = ChuTu1Used;
            chuTu2Used = ChuTu2Used;
            phuTu1Used = PhuTu1Used;
            phuTu2Used = PhuTu2Used;
            phuTu3Used = PhuTu3Used;
            phuTu4Used = PhuTu4Used;
            vuKhi = VuKhi;
            phongCu = PhongCu;
            trangSuc = TrangSuc;
            if (shouldSaveSlots)
            {
                isDirty2 = false;
            }
        }

        if (shouldSaveSlots)
        {
            try
            {
                await db.EquipmentManagers.Where(x => x.CharId == CharId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.DonThuatUsed, donThuatUsed)
                        .SetProperty(x => x.ChuTu1Used, chuTu1Used)
                        .SetProperty(x => x.ChuTu2Used, chuTu2Used)
                        .SetProperty(x => x.PhuTu1Used, phuTu1Used)
                        .SetProperty(x => x.PhuTu2Used, phuTu2Used)
                        .SetProperty(x => x.PhuTu3Used, phuTu3Used)
                        .SetProperty(x => x.PhuTu4Used, phuTu4Used)
                        .SetProperty(x => x.VuKhi, vuKhi)
                        .SetProperty(x => x.PhongCu, phongCu)
                        .SetProperty(x => x.TrangSuc, trangSuc));
            }
            catch
            {
                lock (equipmentLock)
                {
                    isDirty2 = true;
                }

                throw;
            }
        }
    }

    public void AcceptChanges()
    {
        lock (equipmentLock)
        {
            tracking.Clear();
            isDirty1 = false;
        }
    }

}
