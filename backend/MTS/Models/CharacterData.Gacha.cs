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
    public GachaResult? Gacha(GachaModel request)
    {
        RewardSnapshot reward = new RewardSnapshot();
        List<ItemRef> listItemRef = new List<ItemRef>();

        int require = GameConfig.PageGachaWithRequireValue[request.TypeGacha];
        int gachaCount = 1;
        if (request.IsGacha10)
        {
            require *= 10;
            gachaCount = 10;
        }

        if (this.MoneyVip < require)
        {
            Console.WriteLine("chó hack moneyvip");
            throw new Exception(Strings.NotEnoughMoney);
        }

        int newTienNgoc = this.MoneyVip - require;

        for (int i = 0; i < gachaCount; i++)
        {
            if (request.TypeGacha == Game.Gacha.ETypeGacha.ThanThong)
            {
                var item = RollThanThong();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.CongPhap)
            {
                var item = RollCongPhap();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.DanDuoc)
            {
                var item = RollDanDuoc();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.DuocLieu)
            {
                var item = RollDuocLieu();
                if (item != null)
                    listItemRef.Add(item);
            }
            else if (request.TypeGacha == Game.Gacha.ETypeGacha.VatLieu)
            {
                var item = RollVatLieu();
                if (item != null)
                    listItemRef.Add(item);
            }
        }

        listItemRef = ConsolidateItemRefs(listItemRef);
        this.InventoryManager.AddToInventory(listItemRef);
        reward.ItemRefs = listItemRef;
        this.SetMoneyVip(newTienNgoc);
        AddSpendMoneyVip(require, SpendFor.Gacha);
        return new GachaResult()
        {
            Success = true,
            Reward = reward,
            NewTienNgoc = newTienNgoc,
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

    private ItemRef RollThanThong()
    {
        // DiaGiaiHa: 6%, DiaGiaiTrung: 5%, DiaGiaiThuong: 4% (total 15%)
        // ThienGiaiHa: 3%, ThienGiaiTrung: 2%, ThienGiaiThuong: 1% (total 6%)
        // Remaining 79% distributed among NhanGiaiHa, NhanGiaiTrung, NhanGiaiThuong
        // Constraint: NhanGiaiThuong < NhanGiaiTrung < NhanGiaiHa
        // NhanGiaiHa: 31.6%, NhanGiaiTrung: 26.35%, NhanGiaiThuong: 21.05% (6:5:4 ratio)

        double roll = Random.Shared.NextDouble() * 100;
        Game.BaseSkill.ETierSkill tier;

        if (roll < 6) tier = Game.BaseSkill.ETierSkill.DiaGiaiHa;
        else if (roll < 11) tier = Game.BaseSkill.ETierSkill.DiaGiaiTrung;
        else if (roll < 15) tier = Game.BaseSkill.ETierSkill.DiaGiaiThuong;
        else if (roll < 18) tier = Game.BaseSkill.ETierSkill.ThienGiaiHa;
        else if (roll < 20) tier = Game.BaseSkill.ETierSkill.ThienGiaiTrung;
        else if (roll < 21) tier = Game.BaseSkill.ETierSkill.ThienGiaiThuong;
        else if (roll < 52.6) tier = Game.BaseSkill.ETierSkill.NhanGiaiHa;
        else if (roll < 78.95) tier = Game.BaseSkill.ETierSkill.NhanGiaiTrung;
        else tier = Game.BaseSkill.ETierSkill.NhanGiaiThuong;

        // Try to get an item from the rolled tier, with fallback to higher tiers
        var tierOrder = new[]
        {
            tier, Game.BaseSkill.ETierSkill.NhanGiaiHa, Game.BaseSkill.ETierSkill.NhanGiaiTrung,
            Game.BaseSkill.ETierSkill.NhanGiaiThuong
        };
        foreach (var checkTier in tierOrder)
        {
            var validThanThongs = GameConfig.NDCPTTManager.ThanThongs
                .Where(x => x.eTierSkill == checkTier).ToList();

            if (validThanThongs.Count > 0)
            {
                var selected = validThanThongs[Random.Shared.Next(validThanThongs.Count)];
                return new ItemRef((ushort)selected.Id, ItemType.ThanThong, 1);
            }
        }

        return null;
    }

    private ItemRef RollCongPhap()
    {
        // DiaGiaiHa: 6%, DiaGiaiTrung: 5%, DiaGiaiThuong: 4% (total 15%)
        // ThienGiaiHa: 3%, ThienGiaiTrung: 2%, ThienGiaiThuong: 1% (total 6%)
        // Remaining 79% distributed among NhanGiaiHa, NhanGiaiTrung, NhanGiaiThuong
        // Constraint: NhanGiaiThuong < NhanGiaiTrung < NhanGiaiHa
        // NhanGiaiHa: 31.6%, NhanGiaiTrung: 26.35%, NhanGiaiThuong: 21.05% (6:5:4 ratio)

        double roll = Random.Shared.NextDouble() * 100;
        Game.BaseSkill.ETierSkill tier;

        if (roll < 6) tier = Game.BaseSkill.ETierSkill.DiaGiaiHa;
        else if (roll < 11) tier = Game.BaseSkill.ETierSkill.DiaGiaiTrung;
        else if (roll < 15) tier = Game.BaseSkill.ETierSkill.DiaGiaiThuong;
        else if (roll < 18) tier = Game.BaseSkill.ETierSkill.ThienGiaiHa;
        else if (roll < 20) tier = Game.BaseSkill.ETierSkill.ThienGiaiTrung;
        else if (roll < 21) tier = Game.BaseSkill.ETierSkill.ThienGiaiThuong;
        else if (roll < 52.6) tier = Game.BaseSkill.ETierSkill.NhanGiaiHa;
        else if (roll < 78.95) tier = Game.BaseSkill.ETierSkill.NhanGiaiTrung;
        else tier = Game.BaseSkill.ETierSkill.NhanGiaiThuong;

        // Try to get an item from the rolled tier, with fallback to higher tiers
        var tierOrder = new[]
        {
            tier, Game.BaseSkill.ETierSkill.NhanGiaiHa, Game.BaseSkill.ETierSkill.NhanGiaiTrung,
            Game.BaseSkill.ETierSkill.NhanGiaiThuong
        };
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

    private ItemRef RollDanDuoc()
    {
        // LinhDan: 5% Lục, 7% Ngũ, 10% Tứ
        // Recipe: 2% Lục, 5% Ngũ, 7% Tứ
        // Furnace: 1% Lục, 3% Ngũ, 5% Tứ
        // Constraint: Tâm < Nhị < Nhất (always), LinhDan > Recipe > Furnace (always)

        double roll = Random.Shared.NextDouble() * 100;
        ItemType type;
        MTS.Game.LuyenDan.Tier tier;

        // First determine tier - now extends to NhatPham and NhiPham
        // Constraint: Tâm < Nhị < Nhất with ratio 4:5:6
        if (roll < 8) tier = MTS.Game.LuyenDan.Tier.LucPham; // 5% + 2% + 1% = 8%
        else if (roll < 23) tier = MTS.Game.LuyenDan.Tier.NguPham; // 7% + 5% + 3% = 15%, cumulative 23%
        else if (roll < 42) tier = MTS.Game.LuyenDan.Tier.TuPham; // 10% + 7% + 5% = 22%, cumulative 42%
        else if (roll < 56.33) tier = MTS.Game.LuyenDan.Tier.TamPham; // 14.33% for Tam (4 out of 15)
        else if (roll < 74.33) tier = MTS.Game.LuyenDan.Tier.NhiPham; // 18% for Nhi (5 out of 15)
        else tier = MTS.Game.LuyenDan.Tier.NhatPham; // 25.67% for Nhat (6 out of 15)

        // Now determine which type based on the tier
        if (tier == MTS.Game.LuyenDan.Tier.LucPham)
        {
            // LinhDan: 5%, Recipe: 2%, Furnace: 1% -> LinhDan: 62.5%, Recipe: 25%, Furnace: 12.5%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 62.5) type = ItemType.LinhDan;
            else if (typeRoll < 87.5) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.NguPham)
        {
            // LinhDan: 7%, Recipe: 5%, Furnace: 3% -> LinhDan: 46.67%, Recipe: 33.33%, Furnace: 20%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 46.67) type = ItemType.LinhDan;
            else if (typeRoll < 80) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.TuPham)
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

    private ItemRef RollDuocLieu()
    {
        // Herb: 2% Lục, 5% Ngũ, 10% Tứ, then random in Tâm, Nhị, Nhất
        // Recipe: Only 2% Nhị and 5% Nhất (no drops in Lục, Ngũ, Tứ, Tâm)
        // Furnace: Only 2% Nhị and 5% Nhất (no drops in Lục, Ngũ, Tứ, Tâm)
        // Constraint: Recipe < Furnace < Herb (always)

        double roll = Random.Shared.NextDouble() * 100;
        ItemType type;
        MTS.Game.LuyenDan.Tier tier;

        // First determine tier - now extends to NhatPham and NhiPham
        // Lục, Ngũ, Tứ only drop Herb. Tâm drops only Herb. Nhị and Nhất can drop Recipe and Furnace
        if (roll < 2) tier = MTS.Game.LuyenDan.Tier.LucPham; // 2% Herb
        else if (roll < 7) tier = MTS.Game.LuyenDan.Tier.NguPham; // 5% Herb, cumulative 7%
        else if (roll < 17) tier = MTS.Game.LuyenDan.Tier.TuPham; // 10% Herb, cumulative 17%
        else if (roll < 31.33) tier = MTS.Game.LuyenDan.Tier.TamPham; // 14.33% Herb (4 out of 15)
        else if (roll < 49.33)
            tier = MTS.Game.LuyenDan.Tier.NhiPham; // 18% for Nhi (5 out of 15) - includes Recipe 2% + Furnace 2%
        else tier = MTS.Game.LuyenDan.Tier.NhatPham; // 50.67% for Nhat (6 out of 15) - includes Recipe 5% + Furnace 5%

        // Now determine which type based on the tier
        if (tier == MTS.Game.LuyenDan.Tier.LucPham)
        {
            // Lục: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.NguPham)
        {
            // Ngũ: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.TuPham)
        {
            // Tứ: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.TamPham)
        {
            // Tâm: 100% Herb
            type = ItemType.Herb;
        }
        else if (tier == MTS.Game.LuyenDan.Tier.NhiPham)
        {
            // Nhị: Herb (14%), Recipe (2%), Furnace (2%) -> Herb: 77.78%, Recipe: 11.11%, Furnace: 11.11%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 77.78) type = ItemType.Herb;
            else if (typeRoll < 88.89) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }
        else // NhatPham
        {
            // Nhất: Herb (40.67%), Recipe (5%), Furnace (5%) -> Herb: 80.26%, Recipe: 9.87%, Furnace: 9.87%
            double typeRoll = Random.Shared.NextDouble() * 100;
            if (typeRoll < 80.26) type = ItemType.Herb;
            else if (typeRoll < 90.13) type = ItemType.Recipe;
            else type = ItemType.Furnace;
        }

        return RollItemByTypeAndTier(type, tier);
    }

    private ItemRef RollVatLieu()
    {
        // Material: 2% Lục, 5% Ngũ, 10% Tứ (total 17%)
        // Remaining 83% distributed among Tâm, Nhị, Nhất with constraint: Tâm < Nhị < Nhất

        double roll = Random.Shared.NextDouble() * 100;
        MTS.Game.LuyenDan.Tier tier;

        if (roll < 2) tier = MTS.Game.LuyenDan.Tier.LucPham; // 2%
        else if (roll < 7) tier = MTS.Game.LuyenDan.Tier.NguPham; // 5%, cumulative 7%
        else if (roll < 17) tier = MTS.Game.LuyenDan.Tier.TuPham; // 10%, cumulative 17%
        else if (roll < 31.33) tier = MTS.Game.LuyenDan.Tier.TamPham; // 14.33% for Tam (4 out of 15)
        else if (roll < 49.33) tier = MTS.Game.LuyenDan.Tier.NhiPham; // 18% for Nhi (5 out of 15)
        else tier = MTS.Game.LuyenDan.Tier.NhatPham; // 50.67% for Nhat (6 out of 15)

        return RollItemByTypeAndTier(ItemType.Material, tier);
    }


    private ItemRef RollItemByTypeAndTier(ItemType type, MTS.Game.LuyenDan.Tier tier)
    {
        // Fallback tier order - try higher tiers if current tier has no items
        var allTiers = new[]
        {
            MTS.Game.LuyenDan.Tier.NhatPham, MTS.Game.LuyenDan.Tier.NhiPham, MTS.Game.LuyenDan.Tier.TamPham,
            MTS.Game.LuyenDan.Tier.TuPham, MTS.Game.LuyenDan.Tier.NguPham, MTS.Game.LuyenDan.Tier.LucPham
        };
        var tierIndex = System.Array.IndexOf(allTiers, tier);
        var tierOrder = allTiers.Skip(tierIndex).ToArray();

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
