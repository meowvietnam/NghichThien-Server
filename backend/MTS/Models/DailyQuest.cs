using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Localization;
using MTS.Network;

namespace MTS.Models;

[MemoryPackable]
public partial class DailyQuest
{
    [Key] public long Id { get; set; }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    public EDailyQuest EDailyQuest { get; set; }
    public IQuestReward QuestReward { get; set; }
    public bool IsRewardClaimed { get; set; }
    public DateTime LastReload { get; set; }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    [ForeignKey(nameof(CharId))]
    public QuestManager QuestManager { get; set; }

    [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private bool dirty;

    public void Claim()
    {
        if (IsRewardClaimed)
        {
            throw new Exception(Strings.DailyQuestRewardAlreadyClaimed);
        }
        var charData = QuestManager.CharData;
        QuestReward.Claim(charData);
        IsRewardClaimed = true;
        dirty = true;
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (dirty)
        {
            await db.DailyQuests.Where(x => x.Id == Id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.IsRewardClaimed, IsRewardClaimed)
                        .SetProperty(x => x.QuestReward, QuestReward)
                        .SetProperty(x => x.LastReload, LastReload));
            dirty = false;
        }
    }

    public void Reload(IQuestReward reward)
    {
        IsRewardClaimed = false;
        QuestReward = reward;
        LastReload = DateTime.UtcNow;
        dirty = true;
    }

    public void Init(QuestManager questManager)
    {
        QuestManager = questManager;
    }
}