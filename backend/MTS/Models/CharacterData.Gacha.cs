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
using MTS.Game.BaseSkill;
using MTS.IAP;
using MTS.Game.Combat;
using MTS.Network;
using MTS.Utils;
namespace MTS.Models;

public partial class CharacterData
{
    public GachaResult? Gacha(GachaModel request)
    {
        List<ItemRef> listItemRef = new List<ItemRef>();

        int require = GameConfig.PageGachaWithRequireValue[request.TypeGacha];
        int gachaCount = 1;
        if (request.IsGacha10)
        {
            require *= 10;
            gachaCount = 10;
        }
        if (MoneyVip < require)
        {
            throw new InvalidException(Strings.NotEnoughMoney);
        }

        var type = request.TypeGacha;
        int newTienNgoc = MoneyVip - require;
        var totalGacha = GachaManager.Get(type);
        var threshold = GachaManager.Threshold;

        for (int i = 0; i < gachaCount; i++)
        {
            totalGacha++;
            var remaining = threshold - totalGacha % threshold;
            var special = remaining == threshold;
            if (request.TypeGacha == Game.Gacha.ETypeGacha.ThanThong)
            {
                var item = RollThanThong(special);
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.CongPhap)
            {
                var item = RollCongPhap(special);
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.DanDuoc)
            {
                var item = RollDanDuoc(special);
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.DuocLieu)
            {
                var item = RollDuocLieu(special);
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.VatLieu)
            {
                var item = RollVatLieu(special);
                if (item != null)
                    listItemRef.Add(item);
            }
        }

        listItemRef = ConsolidateItemRefs(listItemRef);
        var rewardItems = InventoryManager.AddToInventory(listItemRef);
        GachaManager.Set(type, totalGacha);
        SetMoneyVip(newTienNgoc);
        AddSpendMoneyVipOnline(require, SpendFor.Gacha);
        return new GachaResult()
        {
            Success = true,
            RewardItems = rewardItems,
            NewTienNgoc = newTienNgoc,
            Type = type,
            TotalGacha = totalGacha
        };
    }

    private List<ItemRef> ConsolidateItemRefs(List<ItemRef> items)
    {
        var consolidated = new Dictionary<(ItemType Type, ushort Id), ItemRef>();

        foreach (var item in items)
        {
            var key = (item.Type, item.Id);
            if (consolidated.TryGetValue(key, out var existingItem))
            {
                existingItem.Count = (ushort)(existingItem.Count + item.Count);
            }
            else
            {
                consolidated[key] = new ItemRef(item.Id, item.Type, item.Count);
            }
        }

        return consolidated.Values.ToList();
    }

    private ItemRef? RollThanThong(bool special = false)
    {
        var tier = RollSkillTier(special);
        var tierOrder = GetSkillTierFallbackOrder(tier);
        foreach (var checkTier in tierOrder)
        {
            var validThanThongs = GameConfig.NDCPTTManager.ThanThongs
                .Where(x => x.eTierSkill == checkTier)
                .ToList();
            if (validThanThongs.Count > 0)
            {
                var selected = validThanThongs[Random.Shared.Next(validThanThongs.Count)];
                return new ItemRef((ushort)selected.Id, ItemType.ThanThong, 1);
            }
        }

        return null;
    }

    private ItemRef? RollCongPhap(bool special = false)
    {
        var tier = RollSkillTier(special);
        var tierOrder = GetSkillTierFallbackOrder(tier);
        foreach (var checkTier in tierOrder)
        {
            var validCongPhaps = GameConfig.NDCPTTManager.CongPhaps
                .Where(x => x.eTierSkill == checkTier).ToList();

            if (validCongPhaps.Count > 0)
            {
                var selected = validCongPhaps[Random.Shared.Next(validCongPhaps.Count)];
                return new ItemRef((ushort)selected.Id, ItemType.CongPhap, 1);
            }
        }

        return null;
    }

    private ETierSkill RollSkillTier(bool special)
    {
        if (special)
        {
            return new[] { ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong }.Random();
        }

        return new ItemRateRandomizer<ETierSkill>()
            .Add(ETierSkill.NhanGiaiThuong, 70)
            .Add(ETierSkill.DiaGiaiHa, 12)
            .Add(ETierSkill.DiaGiaiTrung, 9.5)
            .Add(ETierSkill.DiaGiaiThuong, 5)
            .Add(ETierSkill.ThienGiaiHa,2)
            .Add(ETierSkill.ThienGiaiTrung, 1)
            .Add(ETierSkill.ThienGiaiThuong, 0.5)
            .Random();
    }

    private ETierSkill[] GetSkillTierFallbackOrder(ETierSkill tier)
    {
        var allTiers = new[]
        {
            ETierSkill.NhanGiaiThuong,
            ETierSkill.DiaGiaiHa,
            ETierSkill.DiaGiaiTrung,
            ETierSkill.DiaGiaiThuong,
            ETierSkill.ThienGiaiHa,
            ETierSkill.ThienGiaiTrung,
            ETierSkill.ThienGiaiThuong
        };
        var tierIndex = Array.IndexOf(allTiers, tier);
        if (tierIndex < 0)
        {
            return allTiers;
        }

        return allTiers.Skip(tierIndex).Concat(allTiers.Take(tierIndex)).ToArray();
    }

    private ItemRef RollDanDuoc(bool special = false)
    {
        // LinhDan: 5% Lục, 7% Ngũ, 10% Tứ
        // Recipe: 2% Lục, 5% Ngũ, 7% Tứ
        // Furnace: 1% Lục, 3% Ngũ, 5% Tứ
        // Constraint: Tâm < Nhị < Nhất (always), LinhDan > Recipe > Furnace (always)

        double roll = Random.Shared.NextDouble() * 100;
        ItemType type;
        Tier tier;

        if (special)
        {
            tier = Tier.LucPham;
        }
        else
        {
            // First determine tier - now extends to NhatPham and NhiPham
            // Constraint: Tâm < Nhị < Nhất with ratio 4:5:6
            if (roll < 8) tier = Tier.LucPham; // 5% + 2% + 1% = 8%
            else if (roll < 23) tier = Tier.NguPham; // 7% + 5% + 3% = 15%, cumulative 23%
            else if (roll < 42) tier = Tier.TuPham; // 10% + 7% + 5% = 22%, cumulative 42%
            else if (roll < 56.33) tier = Tier.TamPham; // 14.33% for Tam (4 out of 15)
            else if (roll < 74.33) tier = Tier.NhiPham; // 18% for Nhi (5 out of 15)
            else tier = Tier.NhatPham; // 25.67% for Nhat (6 out of 15)
        }

        // Now determine which type based on the tier
        if (tier == Tier.LucPham)
        {
            // LinhDan: 5%, Recipe: 2%, Furnace: 1% -> LinhDan: 62.5%, Recipe: 25%, Furnace: 12.5%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 62.5) type = ItemType.LinhDan;
            else if (typeRoll < 87.5) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else if (tier == Tier.NguPham)
        {
            // LinhDan: 7%, Recipe: 5%, Furnace: 3% -> LinhDan: 46.67%, Recipe: 33.33%, Furnace: 20%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 46.67) type = ItemType.LinhDan;
            else if (typeRoll < 80) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else if (tier == Tier.TuPham)
        {
            // LinhDan: 10%, Recipe: 7%, Furnace: 5% -> LinhDan: 45.45%, Recipe: 31.82%, Furnace: 22.73%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 45.45) type = ItemType.LinhDan;
            else if (typeRoll < 77.27) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else // TamPham, NhiPham, NhatPham with constraint: Tâm < Nhị < Nhất, LinhDan > Recipe > Furnace
        {
            // Distribute with ratio LinhDan:Recipe:Furnace = 3:2:1
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 50) type = ItemType.LinhDan;
            else if (typeRoll < 83.33) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }

        return RollItemByTypeAndTier(type, tier);
    }

    private ItemRef RollDuocLieu(bool special = false)
    {
        if (special)
        {
            return RollItemByTypeAndTier(ItemType.Herb, Tier.LucPham);
        }

        var roll = Random.Shared.NextDouble() * 100;
        if (roll < 7)
        {
            var tier = Random.Shared.NextDouble() < 2d / 7d ? Tier.NhiPham : Tier.NhatPham;
            return RollItemByTypeAndTier(ItemType.Recipe, tier);
        }

        if (roll < 14)
        {
            var tier = Random.Shared.NextDouble() < 2d / 7d ? Tier.NhiPham : Tier.NhatPham;
            return RollItemByTypeAndTier(ItemType.Furnace, tier);
        }

        return RollItemByTypeAndTier(ItemType.Herb, RollDuocLieuHerbTier(), Tier.TamPham);
    }

    private ItemRef RollVatLieu(bool special = false)
    {
        if (special)
        {
            return RollItemByTypeAndTier(ItemType.Material, Tier.LucPham);
        }

        return RollItemByTypeAndTier(ItemType.Material, RollVatLieuTier(), Tier.TamPham);
    }

    private Tier RollDuocLieuHerbTier()
    {
        return new ItemRateRandomizer<Tier>()
            .Add(Tier.TamPham, 68)
            .Add(Tier.TuPham, 12)
            .Add(Tier.NguPham, 4)
            .Add(Tier.LucPham, 2)
            .Random();
    }

    private Tier RollVatLieuTier()
    {
        return new ItemRateRandomizer<Tier>()
            .Add(Tier.TamPham, 80)
            .Add(Tier.TuPham, 12)
            .Add(Tier.NguPham, 5)
            .Add(Tier.LucPham, 3)
            .Random();
    }

    private ItemRef RollItemByTypeAndTier(ItemType type, Tier tier, Tier? minTier = null)
    {
        // Fallback tier order - try higher tiers if current tier has no items
        var allTiers = new[]
        {
            Tier.NhatPham, Tier.NhiPham, Tier.TamPham,
            Tier.TuPham, Tier.NguPham, Tier.LucPham
        };
        if (minTier.HasValue)
        {
            allTiers = allTiers.Where(x => x >= minTier.Value).ToArray();
        }

        var tierIndex = Array.IndexOf(allTiers, tier);
        if (tierIndex < 0)
        {
            tierIndex = 0;
        }

        var tierOrder = allTiers.Take(tierIndex + 1).Reverse().Concat(allTiers.Skip(tierIndex + 1)).ToArray();

        foreach (var checkTier in tierOrder)
        {
            if (type == ItemType.LinhDan)
            {
                var validItems = GameConfig.LuyenDanResources.LinhDans
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.LinhDan, 1);
                }
            }
            else if (type == ItemType.Herb)
            {
                var validItems = GameConfig.LuyenDanResources.HerbDatas
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Herb, 1);
                }
            }
            else if (type == ItemType.Recipe)
            {
                var validItems = GameConfig.LuyenDanResources.Recipes
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Recipe, 1);
                }
            }
            else if (type == ItemType.Furnace)
            {
                var validItems = GameConfig.LuyenDanResources.TempFurnaceDatas
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Furnace, 1);
                }
            }
            else if (type == ItemType.Material)
            {
                var validItems = GameConfig.LuyenKhiResources.Materials
                    .Where(x => x.Tier == checkTier).ToList();
                if (validItems.Count > 0)
                {
                    var selected = validItems[Random.Shared.Next(validItems.Count)];
                    return new ItemRef((ushort)selected.Id, ItemType.Material, 1);
                }
            }
        }
        return null;
    }
}
