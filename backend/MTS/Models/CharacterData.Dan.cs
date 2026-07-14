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
            throw new Exception(Strings.LinhDanQuantityInvalid);
        }
        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == id);
        if (linhDan == null)
        {
            throw new Exception(Strings.LinhDanNotFound);
        }

        if (linhDan.ETypeDan == ETypeDan.DanCombat)
        {
            throw new Exception(Strings.LinhDanCombatBuffInvalid);
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
                throw new Exception(Strings.HasFlagTienThienDiMach);
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
            throw new Exception(Strings.LinhDanQuantityInvalid);
        }

        var linhDan = GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == id);
        if (linhDan == null)
        {
            throw new Exception(Strings.LinhDanNotFound);
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
                    throw new Exception(Strings.DanCombatLimitExceeded);
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
            throw new Exception(Strings.InventoryItemNotFound);
        }

        var remaining = item.Count - count;
        if (remaining < 0)
        {
            throw new Exception(Strings.InventoryItemQuantityInvalid);
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
                throw new Exception(Strings.HasFlagTienThienDiMach);
            }

            Stats.Buff(linhDan, count);
            Stats.ClampHp();
            snapshot = new StatsSnapshot(Stats);
        }

        var result = new UseDanResult()
        {
            Success = true,
            Stats = snapshot,
        };
        return result;
    }
}
