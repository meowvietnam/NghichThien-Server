using MTS.Exceptions;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.ItemProvider;
using MTS.Game.LuyenDan;
using MTS.Game.Resources;
using MTS.Game.Social;
using MTS.Localization;
using MTS.Network.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.IAP;
using MTS.Game.Combat;
using MTS.Network;

namespace MTS.Models;

public partial class CharacterData
{
    public void BuffDan(ItemRef linhDanRef)
    {
        var id = linhDanRef.Id;
        var count = linhDanRef.Count;

        if (count == 0)
        {
            throw new InvalidException(Strings.LinhDanQuantityInvalid);
        }

        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == id);
        if (linhDan == null)
        {
            throw new InvalidException(Strings.LinhDanNotFound);
        }

        if (linhDan.ETypeDan == ETypeDan.DanOther)
        {
            throw new InvalidException(Strings.NotSupportItemType);
        }

        if (linhDan.ETypeDan == ETypeDan.DanCombat)
        {
            throw new InvalidException(Strings.LinhDanCombatBuffInvalid);
        }

        var buffType = linhDan.EBuffType;
        if (buffType != EDanBuffType.None)
        {
            bool isTuViType = buffType switch
            {
                EDanBuffType.TuVi => true,
                EDanBuffType.TuViVaDanDocDuoiTrucCo => true,
                EDanBuffType.TuViVaDanDocDuoiKetDan => true,
                EDanBuffType.TuViVaDanDocDuoiNguyenAnh => true,
                EDanBuffType.TuViVaDanDocDuoiHoaThan => true,
                _ => false
            };
            if (isTuViType && Stats.Ability.HasFlag(Ability.TienThienDiMach))
            {
                throw new InvalidException(Strings.HasFlagTienThienDiMach);
            }

            Stats.Buff(linhDan, count);
            Stats.ClampHp();
        }

        InventoryManager.Decrease(linhDanRef);
    }

    public UseDanResult UseDan(UseDanRequest request)
    {
        var id = request.Id;
        var count = request.Count;

        if (count == 0)
        {
            throw new InvalidException(Strings.LinhDanQuantityInvalid);
        }

        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == id);
        if (linhDan == null)
        {
            throw new InvalidException(Strings.LinhDanNotFound);
        }

        var type = linhDan.ETypeDan;
        var level = Progression.Level;
        var danCombatUseds = EquipmentManager.DanCombatUseds;
        if (type == ETypeDan.DanCombat)
        {
            var danCombatRef = new DanCombatUsed()
            {
                Id = id
            };
            if (danCombatUseds.TryGetValue(danCombatRef, out var used))
            {
                if (used.Count + count > level)
                {
                    throw new InvalidException(Strings.DanCombatLimitExceeded);
                }
            }
        }

        var linhDans = InventoryManager.LinhDans;
        var linhDanRef = new ItemRef()
        {
            Id = id,
            Type = ItemType.LinhDan
        };
        if (!linhDans.TryGetValue(linhDanRef, out var item))
        {
            throw new InvalidException(Strings.InventoryItemNotFound);
        }

        var remaining = item.Count - count;
        if (remaining < 0)
        {
            throw new InvalidException(Strings.InventoryItemQuantityInvalid);
        }

        if (type == ETypeDan.DanOther)
        {
            throw new InvalidException(Strings.NotSupportItemType);
        }

        if (type == ETypeDan.DanCombat)
        {
            var danCombatRef = new DanCombatUsed()
            {
                Id = id
            };
            if (danCombatUseds.TryGetValue(danCombatRef, out var used))
            {
                used.Count += count;
                EquipmentManager.Update(used);
            }
            else
            {
                danCombatRef.Count = count;
                EquipmentManager.Add(danCombatRef);
            }
        }

        InventoryManager.Decrease(new ItemRef()
        {
            Type = ItemType.LinhDan,
            Id = id,
            Count = count,
        });
        var buffType = linhDan.EBuffType;
        StatsSnapshot? snapshot = null;

        if (buffType != EDanBuffType.None)
        {
            bool isTuViType = buffType switch
            {
                EDanBuffType.TuVi => true,
                EDanBuffType.TuViVaDanDocDuoiTrucCo => true,
                EDanBuffType.TuViVaDanDocDuoiKetDan => true,
                EDanBuffType.TuViVaDanDocDuoiNguyenAnh => true,
                EDanBuffType.TuViVaDanDocDuoiHoaThan => true,
                _ => false
            };

            if (isTuViType && Stats.Ability.HasFlag(Ability.TienThienDiMach))
            {
                throw new InvalidException(Strings.HasFlagTienThienDiMach);
            }

            Stats.Buff(linhDan, count);
            Stats.ClampHp();
            snapshot = new StatsSnapshot(Stats);
        }

        var result = new UseDanResult()
        {
            Success = true,
            Stats = snapshot,
            Message = $"{linhDan.Description} x{count}",
            CurrentExp = Progression.CurrentExp,
            CamNgos = InventoryManager.CamNgos
        };
        return result;
    }
}