using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Network.Models;
using static MTS.Utils.Utils;
using MTS.Utils;

namespace MTS.Models;

public class DongPhu
{
    public int CharId { get; set; }
    public int LikeCount { get; private set; }
    public string Name { get; set; }
    public DongPhuLevel Level { get; set; }
    public int CurrentWateringHelpCount { get; private set; }
    public DateTime LastReset { get; set; }
    public PlantGarden Garden { get; set; } = new();
    [MemoryPackIgnore] public DongPhuShop Shop { get; set; } = new();
    [MemoryPackIgnore] public DongPhuInventory Inventory { get; set; } = new();
    [MemoryPackIgnore] public HashSet<DongPhuLike> Likes { get; set; } = new();
    [MemoryPackIgnore] [NotMapped] public Queue<DongPhuHistory> Histories { get; set; } = new();
    [MemoryPackIgnore] [NotMapped] private List<DongPhuHistory> addedHistories = new();
    [MemoryPackIgnore] [NotMapped] private bool dirty;
    [MemoryPackIgnore] [NotMapped] private Modify<DongPhuLike> likesTracking = new();
    [MemoryPackIgnore] [NotMapped] private object dongPhuLock = new();
    [MemoryPackIgnore] [NotMapped] private DongPhuConfig config;

    public void FirstInit(DongPhuConfig config)
    {
        this.config = config;
        Shop.FirstInit();
        Garden.SetConfig(config);
        Garden.InitLevel(Level);
        Garden.FirstInit();
    }

    public void Init(DongPhuConfig config)
    {
        ResetDaily();
        this.config = config;
        Garden.SetConfig(config);
        Garden.InitLevel(Level);
    }

    public void TryUpgrade()
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            var nextLevel = config.GetNextLevel(Level);
            Level = nextLevel.Level;
            dirty = true;
            Garden.SetLevel(Level);
        }
    }

    public DongPhuLevelConfig LevelConfig()
    {
        return config.Levels.First(x => x.Level == Level);
    }

    public int MaxWateringHelperCount()
    {
        return LevelConfig().MyMaxWaterHelpPerDay;
    }

    public bool IsLimitWateringHelperCount()
    {
        return CurrentWateringHelpCount >= MaxWateringHelperCount();
    }

    public void AddLike(DongPhuLike like, CharacterData charData)
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            if (!CanLike(like))
            {
                throw new InvalidException("Không thể thích Động phủ người chơi này");
            }

            Likes.Add(like);
            likesTracking.Add(like);
            LikeCount++;
            AddHistory($"{charData.Name} vừa thích Động phủ của bạn");
            dirty = true;
        }
    }

    private void ResetDaily()
    {
        lock (dongPhuLock)
        {
            if (IsToday(LastReset))
            {
                return;
            }

            LastReset = DateTime.UtcNow;
            SetCurrentWateringHelpCount(0);
            Garden.ResetDaily();
            Shop.ResetDaily();
            DongPhuManager.OnResetDaily(this);
        }
    }

    public void TryHarvest(HarvestRequest request, CharacterData charData)
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            var (rewards,slot) = Garden.TryHarvest(request, charData);
            foreach (var reward in rewards)
            {
                reward.Claim(charData);
            }
            AddHistory($"Bạn đã thu hoạch {slot.GetName()} và nhận được {string.Join(", ", rewards.Select(x => x.GetText()))}");
        }
    }

    public void HelpWatering(WateringHelpRequest request, CharacterData helper, DongPhu target)
    {
        lock (dongPhuLock)
        {
            if (IsLimitWateringHelperCount())
            {
                throw new InvalidException("Đã đạt giới hạn tưới trong ngày");
            }
            target.TryWatering(request, helper);
            var rewards = RandomRewardWatering(target);
            AddHistory($"Bạn đã tưới nước hộ {target.Name} và nhận được {string.Join(", ", rewards.Select(x => x.GetText()))}");
            SetCurrentWateringHelpCount(CurrentWateringHelpCount + 1);
        }
    }

    private List<IDongPhuReward> RandomRewardWatering(DongPhu helped)
    {
        ItemRateRandomizer<IDongPhuReward> randomRewards = new ItemRateRandomizer<IDongPhuReward>(new []
        {
            new ItemRate<IDongPhuReward>(new DongPhuRewardMoney(), 85),
            new ItemRate<IDongPhuReward>()
        });
    }
    public void TryWatering(WateringHelpRequest request, CharacterData helper)
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            var (slot, reduceTime) = Garden.TryWatering(request, helper);
            AddHistory($"{helper.Name} tưới {slot.GetName()} giảm {reduceTime.Minutes} phút sinh trưởng");
        }
    }

    public void AddHistory(string log)
    {
        ResetDaily();
        var history = new DongPhuHistory()
        {
            CharId = CharId,
            Log = log,
        };
        lock (dongPhuLock)
        {
            addedHistories.Add(history);
            Histories.Enqueue(history);
            if (Histories.Count > 50)
            {
                Histories.Dequeue();
            }
        }
    }

    public void SetCurrentWateringHelpCount(int count)
    {
        if (CurrentWateringHelpCount != count)
        {
            CurrentWateringHelpCount = count;
            dirty = true;
        }
    }

    public void SaveHistory(AppDbContext db)
    {
        lock (addedHistories)
        {
        }
    }

    public async Task SaveChanges(AppDbContext db)
    {
    }

    public bool CanLike(int charId)
    {
        var like = new DongPhuLike()
        {
            OwnerCharId = CharId,
            OtherCharId = charId,
        };
        return CanLike(like);
    }

    private bool CanLike(DongPhuLike like)
    {
        return like.OtherCharId != CharId && Likes.Contains(like);
    }
}