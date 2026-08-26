using MemoryPack;
using MTS.Exceptions;
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
                Name = "Phàm cấp",
                Level = DongPhuLevel.None,
                SlotMaxWaterHelpPerDay = 0,
                MyMaxWaterHelpPerDay = 0,
                ReduceTime = TimeSpan.FromMinutes(0),
                RequireMoneyVip = 0,
            },
            new()
            {
                Name = "Hạ cấp",
                Level = DongPhuLevel.Ha,
                SlotMaxWaterHelpPerDay = 3,
                MyMaxWaterHelpPerDay = 3,
                ReduceTime = TimeSpan.FromMinutes(10),
                RequireMoneyVip = 10_000,
            },
            new()
            {
                Name = "Trung cấp",
                Level = DongPhuLevel.Trung,
                SlotMaxWaterHelpPerDay = 5,
                MyMaxWaterHelpPerDay = 5,
                ReduceTime = TimeSpan.FromMinutes(30),
                RequireMoneyVip = 20_000,
            },
            new()
            {
                Name = "Thượng cấp",
                Level = DongPhuLevel.Thuong,
                SlotMaxWaterHelpPerDay = 8,
                MyMaxWaterHelpPerDay = 8,
                ReduceTime = TimeSpan.FromMinutes(60),
                RequireMoneyVip = 50_000,
            }
        };
        var seeds = new DongPhuSeedConfig[]
        {
            new ()
            {
                Tier = Tier.NhatPham,
                GrowthDuration = TimeSpan.FromHours(4),
            },
            new ()
            {
                Tier = Tier.NhiPham,
                GrowthDuration = TimeSpan.FromHours(8),
            },
            new ()
            {
                Tier = Tier.TamPham,
                GrowthDuration = TimeSpan.FromHours(12),
            },
            new ()
            {
                Tier = Tier.TuPham,
                GrowthDuration = TimeSpan.FromHours(16),
            },
            new ()
            {
                Tier = Tier.NguPham,
                GrowthDuration = TimeSpan.FromHours(20),
            },
            new ()
            {
                Tier = Tier.LucPham,
                GrowthDuration = TimeSpan.FromHours(24),
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