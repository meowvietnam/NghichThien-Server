using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Localization;
using Newtonsoft.Json;

namespace MTS.Models;

[MemoryPackable]
public partial class ArenaManager
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    public int CurrentArenaCount { get; private set; }
    public int Points { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }

    public List<RankReward> Rewards { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore] [JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private bool dirty;

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public void SetCurrentArenaCount(int count)
    {
        if (CurrentArenaCount != count)
        {
            CurrentArenaCount = count;
            dirty = true;
        }
    }

    public void SetPoints(int points)
    {
        if (Points != points)
        {
            Points = points;
            dirty = true;
        }
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (dirty)
        {
            await db.ArenaManagers
                .Where(x => x.CharId == CharId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.Points, Points)
                        .SetProperty(x => x.CurrentArenaCount, CurrentArenaCount));
            dirty = false;
        }
    }

    public void Init()
    {
        var values = Enum.GetValues<ETypeArenaRank>().ToList();
        values.Remove(ETypeArenaRank.None);
        Rewards = values.Select(x => new RankReward()
        {
            Type = x,
            IsClaim = false,
            CharId =  CharId,
            ArenaManager = this
        }).ToList();
    }
    public async Task<int> Claim(long id)
    {
        var reward = Rewards.FirstOrDefault(x => x.Id == id);
        if (reward == null)
        {
            throw new Exception(Strings.ArenaRewardNotFound);
        }

        if (reward.IsClaim)
        {
            throw new Exception(Strings.ArenaRewardAlreadyClaimed);
        }

        if (GameConfig.TypeRankArenaWithValue[reward.Type] > Points)
        {
            throw new Exception(Strings.ArenaRankNotReached);
        }
        reward.IsClaim = true;
        await using var db = await Program.CreateDbAsync();
        await db.RankRewards
            .Where(x => x.Id == id && x.CharId == CharId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.IsClaim, reward.IsClaim));
        var moneyVip = GameConfig.TypeRankArenaWithReward[reward.Type];
        var newMoneyVip = CharData.MoneyVip + moneyVip;
        CharData.SetMoneyVip(newMoneyVip);
        return moneyVip;
    }

    public void CopyFrom(ArenaManager clone)
    {
        Points = clone.Points;
        CurrentArenaCount = clone.CurrentArenaCount;
    }
}
