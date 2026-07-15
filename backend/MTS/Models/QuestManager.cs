using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Localization;
using Network;
using Network.Extensions;

namespace MTS.Models;

[MemoryPackable]
public partial class QuestManager
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    public int CurrentMainQuest { get; private set; } = 0;
    public int CurrentCountSideQuest { get; private set; } = 0;

    [NotMapped] [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore]
    private bool dirty = false;

    [ForeignKey(nameof(CharId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public CharacterData CharData { get; set; }

    [NotMapped] public Dictionary<EDailyQuest, List<DailyQuest>> DailyQuests { get; set; } = new();

    public async Task Load(AppDbContext db)
    {
        DailyQuests = await db.DailyQuests
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .GroupBy(x => x.EDailyQuest)
            .ToDictionaryAsync(x => x.Key, x => x.ToList());
        foreach (var dailyQuest in DailyQuests.Values)
        {
            dailyQuest.ForEach(x => x.Init(this));
        }
    }

    public void SetCurrentMainQuest(int mainQuest)
    {
        if (CurrentMainQuest != mainQuest)
        {
            CurrentMainQuest = mainQuest;
            dirty = true;
        }
    }

    public void SetCurrentCountSideQuest(int sideQuest)
    {
        if (CurrentCountSideQuest != sideQuest)
        {
            CurrentCountSideQuest = sideQuest;
            dirty = true;
        }
    }

    public void CopyFrom(QuestManager other)
    {
        CurrentMainQuest = other.CurrentMainQuest;
        CurrentCountSideQuest = other.CurrentCountSideQuest;
        dirty = false;
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public async Task SaveChanges(AppDbContext db)
    {
        foreach (var quests in DailyQuests.Values)
        {
            foreach (var mainQuest in quests)
            {
                await mainQuest.SaveChanges(db);
            }
        }

        if (!dirty) return;
        await db.QuestManagers
            .Where(x => x.CharId == CharId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.CurrentMainQuest, CurrentMainQuest)
                    .SetProperty(x => x.CurrentCountSideQuest, CurrentCountSideQuest));
        dirty = false;
    }

    public void ClaimDailyQuest(long id)
    {
        var quest = DailyQuests.SelectMany(x => x.Value)
            .FirstOrDefault(x => x.Id == id);
        if (quest == null)
        {
            throw new Exception(Strings.DailyQuestNotFound);
        }

        quest.Claim();
    }

    public List<DailyQuest> Reload(EDailyQuest edailyQuest)
    {
        var quests = DailyQuests[edailyQuest]
            .Where(x => !x.IsRewardClaimed)
            .ToArray();
        var totalQuest = quests.Length;
        var items = GetRandomItems(totalQuest, 1);
        var isSpecials = quests.Select(x => x.QuestReward is QuestMoneyVipReward)
            .OrderBy(x => Random.Shared.Next())
            .ToArray();
        for (var i = 0; i < quests.Length; i++)
        {
            var quest = quests[i];
            var isSpecial = isSpecials[i];
            var item = items[i];
            IQuestReward reward;
            if (isSpecial)
            {
                reward = new QuestMoneyVipReward(item);
            }
            else
            {
                reward = new QuestMoneyReward(item);
            }

            quest.Reload(reward);
        }

        return DailyQuests[edailyQuest];
    }

    public void ReloadDailyQuests()
    {
        var totalQuest = DailyQuests.First().Value.Count;
        var items = GetRandomItems(totalQuest, DailyQuests.Keys.Count);
        var i = 0;
        foreach (var quests in DailyQuests.Values)
        {
            var questSpecials = new bool[totalQuest];
            questSpecials[0] = true;
            questSpecials = questSpecials.OrderBy(x => Random.Shared.Next()).ToArray();
            for (var j = 0; j < quests.Count; j++)
            {
                var index = i * totalQuest + j;
                var item = items[index];
                var special = questSpecials[j];
                IQuestReward reward;
                if (!special)
                {
                    reward = new QuestMoneyReward(item);
                }
                else
                {
                    reward = new QuestMoneyVipReward(item);
                }

                quests[j].Reload(reward);
            }

            i++;
        }
    }

    private ushort RandomCount()
    {
        return (ushort)Random.Shared.Next(1, 4);
    }

    private ItemRef[] GetRandomItems(int totalQuest, int keys)
    {
        var herbs = GameConfig.LuyenDanResources.HerbDatas;
        var materials = GameConfig.LuyenKhiResources.Materials;
        var randoms = herbs
            .Select(x => new ItemRef()
            {
                Type = ItemType.Herb,
                Id = (ushort)x.Id,
                Count = RandomCount()
            })
            .Concat(materials.Select(x => new ItemRef()
            {
                Type = ItemType.Material,
                Id = (ushort)x.Id,
                Count = RandomCount()
            }))
            .OrderBy(x => Random.Shared.Next())
            .Take(totalQuest * keys)
            .ToArray();
        return randoms;
    }

    public void InitDailyQuests()
    {
        var totalQuest = 5;
        var values = Enum.GetValues<EDailyQuest>();
        var randomItems = GetRandomItems(totalQuest, values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var questSpecials = new bool[totalQuest];
            questSpecials[0] = true;
            questSpecials = questSpecials.OrderBy(x => Random.Shared.Next()).ToArray();
            var i1 = i;
            var quests = questSpecials
                .Select((x, j) =>
                {
                    var index = i1 * totalQuest + j;
                    var item = randomItems[index];
                    if (!x)
                    {
                        return (IQuestReward)new QuestMoneyReward(item);
                    }

                    return new QuestMoneyVipReward(item);
                })
                .Select(x => new DailyQuest()
                {
                    EDailyQuest = value,
                    QuestReward = x,
                    LastReload = DateTime.UtcNow,
                    QuestManager = this
                })
                .ToList();
            DailyQuests[value] = quests;
        }
    }
}