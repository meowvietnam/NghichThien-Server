using MemoryPack;
using MTS.Exceptions;
using MTS.Game.BaseSkill;
using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.DP;

[MemoryPackable]
public partial class DongPhuConfig
{
    public DongPhuLevelConfig[] Levels { get; private set; }
    public DongPhuSeedConfig[] Seeds { get; private set; }

    public DongPhuLevelConfig GetNextLevel(DongPhuLevel level)
    {
        var index = Array.FindIndex(Levels, x => x.Level == level);
        if (index < 0)
        {
            throw new InvalidException("Level không chính xác");
        }
        var nextLevelIndex = index + 1;
        if (nextLevelIndex >= Levels.Length)
        {
            throw new InvalidException("Level đã đạt tối đa");
        }

        return Levels[nextLevelIndex];
    }

    public static DongPhuConfig Default()
    {
        var levels = new DongPhuLevelConfig[]
        {
            new()
            {
                Name = "Phàm Giai",
                Level = DongPhuLevel.None,
                SlotMaxWaterHelpPerDay = 0,
                MyMaxWaterHelpPerDay = 0,
                ReduceTime = TimeSpan.FromMinutes(0),
                RequireMoneyVip = 0,
                
                Color = new Color()
                {
                    R = 0.45f,
                    G = 0.45f,
                    B = 0.45f,
                    A = 1f
                },
                Alpha = 1,
            },
            new()
            {
                Name = "Hoàng Giai",
                Level = DongPhuLevel.Hoang,
                SlotMaxWaterHelpPerDay = 3,
                MyMaxWaterHelpPerDay = 3,
                ReduceTime = TimeSpan.FromMinutes(10),
                RequireMoneyVip = 5_000,
                Color = new Color()
                {
                    R = 0.30f,
                    G = 0.65f,
                    B = 0.90f,
                    A = 1f
                },
                Alpha = 1,
                RandomItem = new ItemRef()
                {
                    Type = ItemType.CongPhap,
                    Count = 1
                },
                TierSkill = [ETierSkill.DiaGiaiHa, ETierSkill.DiaGiaiTrung, ETierSkill.DiaGiaiThuong],
                Amount = 20_000,
            },
            new()
            {
                Name = "Huyền Giai",
                Level = DongPhuLevel.Huyen,
                SlotMaxWaterHelpPerDay = 5,
                MyMaxWaterHelpPerDay = 5,
                ReduceTime = TimeSpan.FromMinutes(20),
                RequireMoneyVip = 20_000,
                Color = new Color()
                {
                    R = 0.55f,
                    G = 0.35f,
                    B = 0.90f,
                    A = 1f
                },
                Alpha = 2,
                RandomItem = new ItemRef()
                {
                    Type = ItemType.CongPhap,
                    Count = 1
                },
                TierSkill = [ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong],
                Amount = 100_000,
            },
            new()
            {
                Name = "Địa Giai",
                Level = DongPhuLevel.Dia,
                SlotMaxWaterHelpPerDay = 7,
                MyMaxWaterHelpPerDay = 7,
                ReduceTime = TimeSpan.FromMinutes(40),
                RequireMoneyVip = 40_000,
                Color = new Color()
                {
                    R = 0.85f,
                    G = 0.50f,
                    B = 0.20f,
                    A = 1f
                },
                Alpha = 3,
                RandomItem = new ItemRef()
                {
                    Type = ItemType.CongPhap,
                    Count = 2
                },
                TierSkill = [ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong],
                Amount = 200_000
            },
            new()
            {
                Name = "Thiên Giai",
                Level = DongPhuLevel.Thien,
                SlotMaxWaterHelpPerDay = 9,
                MyMaxWaterHelpPerDay = 9,
                ReduceTime = TimeSpan.FromMinutes(80),
                RequireMoneyVip = 80_000,
                Color = new Color()
                {
                    R = 1.00f,
                    G = 0.78f,
                    B = 0.20f,
                    A = 1f
                },
                Alpha = 4,
                RandomItem = new ItemRef()
                {
                    Type = ItemType.CongPhap,
                    Count = 4
                },
                TierSkill = [ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong],
                Amount = 400_000,
            },
            new()
            {
                Name = "Thần Giai",
                Level = DongPhuLevel.Than,
                SlotMaxWaterHelpPerDay = 12,
                MyMaxWaterHelpPerDay = 12,
                ReduceTime = TimeSpan.FromMinutes(160),
                RequireMoneyVip = 160_000,
                Color = new Color()
                {
                    R = 1.00f,
                    G = 0.35f,
                    B = 0.65f,
                    A = 1f
                },
                Alpha = 5,
                RandomItem = new ItemRef()
                {
                    Type = ItemType.CongPhap,
                    Count = 6
                },
                TierSkill = [ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong],
                Amount = 800_000,
            }
        };
        var seeds = new DongPhuSeedConfig[]
        {
            new()
            {
                Tier = Tier.NhatPham,
                GrowthDuration = TimeSpan.FromHours(1),
            },
            new()
            {
                Tier = Tier.NhiPham,
                GrowthDuration = TimeSpan.FromHours(2),
            },
            new()
            {
                Tier = Tier.TamPham,
                GrowthDuration = TimeSpan.FromHours(3),
            },
            new()
            {
                Tier = Tier.TuPham,
                GrowthDuration = TimeSpan.FromHours(6),
            },
            new()
            {
                Tier = Tier.NguPham,
                GrowthDuration = TimeSpan.FromHours(9),
            },
            new()
            {
                Tier = Tier.LucPham,
                GrowthDuration = TimeSpan.FromHours(12),
            },
        };
        var config = new DongPhuConfig()
        {
            Levels = levels,
            Seeds = seeds,
        };
        return config;
    }

    public bool IsLevelGreaterOrEqual(DongPhuLevel levelA, DongPhuLevel levelB)
    {
        var indexA = Array.FindIndex(Levels, x => x.Level == levelA);
        var indexB = Array.FindIndex(Levels, x => x.Level == levelB);
        return indexA >= indexB;
    }
}