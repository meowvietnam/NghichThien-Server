using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class MonsterKill
{
    [Key] [MemoryPackIgnore] public int CharId { get; set; }
    public int Points { get; private set; }
    public List<MonsterRankReward> Rewards { get; private set; } = new();

    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public CharacterData CharData { get; set; }

    [NotMapped] [MemoryPackIgnore] private bool dirty;

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public void SetPoints(int point)
    {
        if (Points != point)
        {
            Points = point;
            dirty = true;
        }
    }

    public void Init()
    {
        Rewards = new()
        {
            new MonsterRankReward()
            {
                CharId = CharId,
                RequirePoints = 100,
                Rewards = new()
                {
                    new MoneyVipMonsterRankReward()
                    {
                        MoneyVip = 100
                    },
                    new ItemMonsterRankReward()
                    {
                        Type = ItemType.ThanThong,
                        Tier = ETierSkill.DiaGiaiHa,
                        Count = 1
                    }
                }
            },
            new MonsterRankReward()
            {
                CharId = CharId,
                RequirePoints = 200,
                Rewards = new()
                {
                    new MoneyVipMonsterRankReward()
                    {
                        MoneyVip = 300
                    },
                    new ItemMonsterRankReward()
                    {
                        Type = ItemType.CongPhap,
                        Tier = ETierSkill.DiaGiaiHa,
                        Count = 1
                    }
                }
            },
            new MonsterRankReward()
            {
                CharId = CharId,
                RequirePoints = 400,
                Rewards = new()
                {
                    new MoneyVipMonsterRankReward()
                    {
                        MoneyVip = 500,
                    },
                    new ItemMonsterRankReward()
                    {
                        Type = ItemType.ThanThong,
                        Tier = ETierSkill.DiaGiaiTrung,
                        Count = 1
                    }
                }
            },
            new MonsterRankReward()
            {
                CharId = CharId,
                RequirePoints = 800,
                Rewards = new()
                {
                    new MoneyVipMonsterRankReward()
                    {
                        MoneyVip = 700
                    },
                    new ItemMonsterRankReward()
                    {
                        Type = ItemType.CongPhap,
                        Tier = ETierSkill.DiaGiaiTrung,
                        Count = 1
                    }
                }
            },
            new MonsterRankReward()
            {
                CharId = CharId,
                RequirePoints = 1600,
                Rewards = new()
                {
                    new MoneyVipMonsterRankReward()
                    {
                        MoneyVip = 900
                    },
                    new ItemMonsterRankReward()
                    {
                        Type = ItemType.ThanThong,
                        Tier = ETierSkill.DiaGiaiThuong,
                        Count = 1
                    }
                }
            },
            new MonsterRankReward()
            {
                CharId = CharId,
                RequirePoints = 3200,
                Rewards = new()
                {
                    new MoneyVipMonsterRankReward()
                    {
                        MoneyVip = 1100
                    },
                    new ItemMonsterRankReward()
                    {
                        Type = ItemType.CongPhap,
                        Tier = ETierSkill.DiaGiaiThuong,
                        Count = 1
                    }
                }
            },
        };
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (!dirty)
        {
            return;
        }

        await db.MonsterKills
            .Where(x => x.CharId == CharId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.Points, Points));
        dirty = false;
    }

    public async Task<ClaimMonsterKillRewardResult> Claim(long id)
    {
        var reward = Rewards.FirstOrDefault(x => x.Id == id);
        if (reward == null)
        {
            throw new InvalidException("Phần thưởng không tồn tại.");
        }

        if (reward.IsClaim)
        {
            throw new InvalidException("Phần thưởng này đã được nhận.");
        }

        if (reward.RequirePoints > Points)
        {
            throw new InvalidException("Bạn chưa đủ điểm để nhận phần thưởng này.");
        }

        foreach (var monsterRankReward in reward.Rewards)
        {
            monsterRankReward.Claim(CharData);
        }

        reward.IsClaim = true;
        await using var db = await Program.CreateDbAsync();
        await db.MonsterRankRewards
            .Where(x => x.Id == id && x.CharId == CharId)
            .ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsClaim, reward.IsClaim));
        return new ClaimMonsterKillRewardResult()
        {
            Success = true,
            Id = reward.Id,
            Rewards = reward.Rewards
        };
    }
}