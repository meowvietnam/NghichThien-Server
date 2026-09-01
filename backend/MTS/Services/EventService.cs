using MTS.Game.BaseSkill;
using MTS.Models;
using MTS.Network.Models;
using static MTS.Utils.Utils;

namespace MTS.Services;

public static class EventService
{
    public static List<Event> CreateEvents()
    {
        var eventList = new List<Event>();
        var startAt = new DateTimeOffset(2026, 08, 31, 0, 0, 0, Utc7).UtcDateTime;
        var endAt = startAt.AddDays(8);
        var typeSkills = Enum.GetValues(typeof(ETypeSkill)) as ETypeSkill[];
        typeSkills = typeSkills.Skip(1).ToArray();
        var configs = new[]
        {
            new
            {
                Amount = 50_000,
                MoneyVip = 6_600,
                Type = ItemType.CongPhap,
                Tiers = new[]
                {
                    ETierSkill.ThienGiaiHa,
                    ETierSkill.ThienGiaiTrung,
                    ETierSkill.ThienGiaiThuong,
                },
                Count = 1,
            },
            new
            {
                Amount = 100_000,
                MoneyVip = 13_800,
                Type = ItemType.CongPhap,
                Tiers = new[]
                {
                    ETierSkill.ThienGiaiHa,
                    ETierSkill.ThienGiaiTrung,
                    ETierSkill.ThienGiaiThuong,
                },
                Count = 2,
            },
            new
            {
                Amount = 200_000,
                MoneyVip = 30_600,
                Type = ItemType.CongPhap,
                Tiers = new[]
                {
                    ETierSkill.ThienGiaiHa,
                    ETierSkill.ThienGiaiTrung,
                    ETierSkill.ThienGiaiThuong,
                },
                Count = 4,
            },
            new
            {
                Amount = 500_000,
                MoneyVip = 78_600,
                Type = ItemType.CongPhap,
                Tiers = new[]
                {
                    ETierSkill.ThienGiaiHa,
                    ETierSkill.ThienGiaiTrung,
                    ETierSkill.ThienGiaiThuong,
                },
                Count = 12,
            },
            new
            {
                Amount = 1_000_000,
                MoneyVip = 162_300,
                Type = ItemType.CongPhap,
                Tiers = new[]
                {
                    ETierSkill.ThienGiaiHa,
                    ETierSkill.ThienGiaiTrung,
                    ETierSkill.ThienGiaiThuong,
                },
                Count = 20
            },
        };
        var milestones = new List<RechargeMilestone1>();
        for (var i = 0; i < typeSkills.Length; i++)
        {
            var length = configs.Length;
            var typeSkill = typeSkills[i];
            for (int j = 0; j < length; j++)
            {
                var config = configs[j];
                var id = i * length + j;
                var rechargeMilestone = new RechargeMilestone1()
                {
                    Id = (ushort)id,
                    Amount = config.Amount,
                    Name = string.Empty,
                    Type = typeSkill,
                    Rewards = new List<IEventReward>()
                    {
                        new MoneyVipEventReward()
                        {
                            MoneyVip = config.MoneyVip,
                        },
                        new SpecialSkillEventReward()
                        {
                            Count = (ushort)config.Count,
                            Tiers = config.Tiers,
                            Type = config.Type,
                            TypeSkill = typeSkill
                        }
                    }
                };
                milestones.Add(rechargeMilestone);
            }
        }

        var gameEvent1 = new RechargeEvent1()
        {
            Milestones = milestones
        };
        var event1 = new Event()
        {
            Id = 0,
            StartAt = startAt,
            EndAt = endAt,
            GameEvent = gameEvent1
        };
        var gameEvent2 = new MilestoneEvent()
        {
            Milestones = new List<EventMilestone>()
            {
                new()
                {
                    Id = 0,
                    Targets =
                    [
                        new ItemEvent()
                        {
                            Id = 0,
                            Count = 100
                        }
                    ],
                    Rewards = new List<IEventReward>()
                    {
                        new MoneyVipEventReward()
                        {
                            MoneyVip = 100,
                        },
                        new SpecialSkillEventReward()
                        {
                            Tiers = [ETierSkill.DiaGiaiTrung],
                            Type = ItemType.ThanThong,
                            Count = 1,
                        }
                    }
                },
                new()
                {
                    Id = 1,
                    Targets =
                    [
                        new ItemEvent()
                        {
                            Id = 0,
                            Count = 200
                        }
                    ],
                    Rewards = new List<IEventReward>()
                    {
                        new MoneyVipEventReward()
                        {
                            MoneyVip = 300,
                        },
                        new SpecialSkillEventReward()
                        {
                            Tiers = [ETierSkill.DiaGiaiTrung],
                            Type = ItemType.CongPhap,
                            Count = 1,
                        }
                    }
                },
                new()
                {
                    Id = 2,
                    Targets =
                    [
                        new ItemEvent()
                        {
                            Id = 0,
                            Count = 400
                        }
                    ],
                    Rewards = new List<IEventReward>()
                    {
                        new MoneyVipEventReward()
                        {
                            MoneyVip = 500,
                        },
                        new SpecialSkillEventReward()
                        {
                            Tiers = [ETierSkill.DiaGiaiThuong],
                            Type = ItemType.ThanThong,
                            Count = 1,
                        }
                    }
                },
                new()
                {
                    Id = 3,
                    Targets =
                    [
                        new ItemEvent()
                        {
                            Id = 0,
                            Count = 800
                        }
                    ],
                    Rewards = new List<IEventReward>()
                    {
                        new MoneyVipEventReward()
                        {
                            MoneyVip = 700,
                        },
                        new SpecialSkillEventReward()
                        {
                            Tiers = [ETierSkill.DiaGiaiThuong],
                            Type = ItemType.CongPhap,
                            Count = 1,
                        }
                    }
                },
                new()
                {
                    Id = 4,
                    Targets =
                    [
                        new ItemEvent()
                        {
                            Id = 0,
                            Count = 1600
                        }
                    ],
                    Rewards = new List<IEventReward>()
                    {
                        new MoneyVipEventReward()
                        {
                            MoneyVip = 900,
                        },
                        new SpecialSkillEventReward()
                        {
                            Tiers = [ETierSkill.ThienGiaiHa],
                            Type = ItemType.ThanThong,
                            Count = 1,
                        }
                    }
                },
                new()
                {
                    Id = 5,
                    Targets =
                    [
                        new ItemEvent()
                        {
                            Id = 0,
                            Count = 3200
                        }
                    ],
                    Rewards = new List<IEventReward>()
                    {
                        new MoneyVipEventReward()
                        {
                            MoneyVip = 1100,
                        },
                        new SpecialSkillEventReward()
                        {
                            Tiers = [ETierSkill.ThienGiaiHa],
                            Type = ItemType.CongPhap,
                            Count = 1,
                        }
                    }
                },
            }
        };
        var event2 = new Event()
        {
            Id = 1,
            StartAt = startAt,
            EndAt = endAt,
            GameEvent = gameEvent2
        };
        eventList.Add(event1);
        eventList.Add(event2);
        return eventList;
    }

    public static List<ItemEvent> GenerateItemEvents()
    {
        return new List<ItemEvent>()
        {
            new()
            {
                Id = 0,
                Count = Random.Shared.Next(1, 6)
            }
        };
    }
}