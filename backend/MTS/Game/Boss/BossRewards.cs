using MTS.Models;
using MTS.Network.Models;
using MTS.Game.LuyenDan;

namespace MTS.Game.Boss;

public static class BossRewards
{
    public const int FirstJoinMoneyVip = 100;
    public const int KillerMoneyVip = 5000;
    public const int ParticipantMoneyVip = 1000;

    // Chances use basis points: 10,000 = 100%.
    public static (int TamPham, int TuPham, int NguPham, int LucPham) GetFurnaceChances(long damage, int maxHp)
    {
        var steps = maxHp > 0 ? (int)Math.Min(320, decimal.Floor((decimal)Math.Max(0, damage) * 1000 / maxHp)) : 0;
        var bonusPerRareTier = steps * 10;
        return (9600 - bonusPerRareTier * 3,
            200 + bonusPerRareTier,
            150 + bonusPerRareTier,
            50 + bonusPerRareTier);
    }

    public static FirstDailyBossReward CreateFirstJoinReward()
    {
        return new FirstDailyBossReward { Rewards = [new MailRewardMoneyVip { MoneyVip = FirstJoinMoneyVip }] };
    }

    public static List<IMailReward> CreateParticipantRewards(BossData boss)
    {
        return [new MailRewardMoneyVip { MoneyVip = ParticipantMoneyVip }];
    }

    public static List<IMailReward> CreateDamageRewards(BossData boss, long damage)
    {
        var chances = GetFurnaceChances(damage, boss.Character.SinhMenh);
        var roll = Random.Shared.Next(10000);
        var tier = roll < chances.TamPham ? Tier.TamPham
            : roll < chances.TamPham + chances.TuPham ? Tier.TuPham
            : roll < chances.TamPham + chances.TuPham + chances.NguPham ? Tier.NguPham
            : Tier.LucPham;
        return [CreateFurnaceReward(tier)];
    }

    public static List<IMailReward> CreateKillerRewards(BossData boss)
    {
        return [CreateFurnaceReward(Tier.LucPham), new MailRewardMoneyVip { MoneyVip = KillerMoneyVip }];
    }

    private static MailRewardFurnace CreateFurnaceReward(Tier tier)
    {
        var furnaces = GameConfig.LuyenDanResources.TempFurnaceDatas.Where(x => x.Tier == tier).ToArray();
        if (furnaces.Length == 0) throw new InvalidOperationException($"Không có đan lô {tier} trong dữ liệu.");
        var furnace = furnaces[Random.Shared.Next(furnaces.Length)];
        return new MailRewardFurnace
        {
            FurnaceData = new FurnaceData { Id = checked((ushort)furnace.Id), Durability = 100 }
        };
    }
}
