using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Network.Models;
using static MTS.Utils.Utils;
using MTS.Utils;
using MTS.Localization;
using Microsoft.EntityFrameworkCore;

namespace MTS.Models;

[MemoryPackable]
public partial class DongPhu
{
    public int CharId { get; set; }
    public int LikeCount { get; private set; }
    public string Name { get; set; }
    public DongPhuLevel Level { get; set; }
    public int CurrentWateringHelpCount { get; private set; }

    public PlantGarden Garden { get; set; } = new();
    [MemoryPackIgnore]
    public DateTime LastReset { get; set; }
    [MemoryPackIgnore] public DongPhuShop Shop { get; set; } = new();
    [MemoryPackIgnore] public DongPhuInventory Inventory { get; set; } = new();
    [MemoryPackIgnore] public HashSet<DongPhuLike> Likes { get; set; } = new();
    [MemoryPackIgnore][NotMapped] public Queue<DongPhuHistory> Histories { get; set; } = new();
    [MemoryPackIgnore][NotMapped] private List<DongPhuHistory> addedHistories = new();
    [MemoryPackIgnore][NotMapped] private bool dirty;
    [MemoryPackIgnore][NotMapped] private Modify<DongPhuLike> likesTracking = new();
    [MemoryPackIgnore][NotMapped] private object dongPhuLock = new();
    [MemoryPackIgnore][NotMapped] private DongPhuConfig config;

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
    public DongPhuShop GetShop()
    {
        ResetDaily();
        return Shop;
    }
    public DongPhuInventory GetInventory()
    {
        ResetDaily();
        return Inventory;
    }
    public Queue<DongPhuHistory> GetHistories()
    {
        ResetDaily();
        return Histories;
    }
    public UpgradeDongPhuResult TryUpgrade(CharacterData charData)
    {
        if (charData.Id != CharId)
        {
            throw new InvalidException("Không thể nâng cấp Động phủ người chơi khác");
        }
        lock (dongPhuLock)
        {
            ResetDaily();
            var nextLevel = config.GetNextLevel(Level);
            var remaining = charData.MoneyVip - nextLevel.RequireMoneyVip;
            if (remaining < 0)
            {
                throw new InvalidException(Strings.NotEnoughMoneyVip);
            }
            Level = nextLevel.Level;
            charData.SetMoneyVip(remaining);
            Garden.SetLevel(Level);
            var log = $"Bạn đã nâng cấp Động phủ {nextLevel.Name}";
            AddHistory(log);
            dirty = true;
            return new UpgradeDongPhuResult()
            {
                Success = true,
                Message = log,
                NewLevel = Level,
                NewMoneyVip = remaining
            };
        }
    }
    public PlantResult Plant(PlantRequest request, CharacterData charData)
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            var seed = Inventory.GetSeed(request.HerbId);
            if (seed == null)
            {
                throw new InvalidException("Hạt giống không tồn tại");
            }
            var (plantSeed, slot) = Garden.Plant(seed, request.SlotIndex, charData);
            Inventory.DecreaseCount(seed, 1);
            var herbData = plantSeed.GetHerbData();
            var log = $"Bạn đã trồng {herbData.Name} tại {slot.GetName()}";
            AddHistory(log);
            return new PlantResult()
            {
                Success = true,
                Message = log,
                PlantSeed = plantSeed,
                UpdatedSeed = seed
            };
        }
    }
    public DongPhuConfig GetConfig()
    {
        return config;
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
    public LikeDongPhuResult Like(CharacterData charData)
    {
        var like = new DongPhuLike()
        {
            OtherCharId = charData.Id,
            OwnerCharId = CharId,
        };
        return AddLike(like, charData);
    }
    private LikeDongPhuResult AddLike(DongPhuLike like, CharacterData charData)
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
            return new LikeDongPhuResult()
            {
                CanLike = false,
                NewLike = LikeCount
            };
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
        }
    }

    public HarvestSlotResult TryHarvest(HarvestRequest request, CharacterData charData)
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            var (rewards, slot) = Garden.TryHarvest(request, charData);
            foreach (var reward in rewards)
            {
                reward.Claim(charData);
            }
            var log = $"Bạn đã thu hoạch {slot.GetName()} và nhận được {string.Join(", ", rewards.Select(x => x.GetText()))}";
            AddHistory(log);
            return new HarvestSlotResult()
            {
                Success = true,
                Message = log,
                SlotIndex = slot.SlotIndex,
                Rewards = rewards,
            };
        }
    }

    public WateringHelpResult HelpWatering(WateringHelpRequest request, CharacterData helper, DongPhu target)
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            if (IsLimitWateringHelperCount())
            {
                throw new InvalidException("Đã đạt giới hạn tưới trong ngày");
            }
            var (slot, reduceTime) = target.TryWatering(request, helper);
            var rewards = RandomRewardWatering(target);
            foreach (var item in rewards)
            {
                item.Claim(helper);
            }
            var log = $"Bạn đã tưới nước hộ {target.Name} và nhận được {string.Join(", ", rewards.Select(x => x.GetText()))}";
            AddHistory(log);
            SetCurrentWateringHelpCount(CurrentWateringHelpCount + 1);
            return new WateringHelpResult()
            {
                Success = true,
                SlotIndex = slot.SlotIndex,
                CurrentWateringHelpCount = CurrentWateringHelpCount,
                ReduceTime = reduceTime,
                Rewards = rewards,
                Message = log
            };
        }
    }

    private List<IDongPhuReward> RandomRewardWatering(DongPhu helped)
    {
        ItemRateRandomizer<IDongPhuReward> randomRewards = new ItemRateRandomizer<IDongPhuReward>(new[]
        {
            new ItemRate<IDongPhuReward>(new DongPhuRewardMoney(Random.Shared.Next(0,1000)), 85),
            new ItemRate<IDongPhuReward>(new DongPhuRewardMoney(Random.Shared.Next(0,100)), 10)
        });
        return new List<IDongPhuReward>()
        {
            randomRewards.Random()
        };
    }
    public (PlantingSlot slot, TimeSpan reduceTime) TryWatering(WateringHelpRequest request, CharacterData helper)
    {
        lock (dongPhuLock)
        {
            ResetDaily();
            var (slot, reduceTime) = Garden.TryWatering(request, helper);
            AddHistory($"{helper.Name} tưới {slot.GetName()} giảm {reduceTime.Minutes} phút sinh trưởng");
            return (slot, reduceTime);
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

    private void SaveHistories(AppDbContext db)
    {

        lock (addedHistories)
        {
            if (addedHistories.Count == 0)
            {
                return;
            }
            db.DongPhuHistories.AddRange(addedHistories);
            addedHistories.Clear();
        }

    }
    private void SaveLikes(AppDbContext db)
    {
        lock (dongPhuLock)
        {
            likesTracking.Apply(db, db.DongPhuLikes);
            likesTracking.Clear();
        }
    }
    public async Task SaveChanges(AppDbContext db)
    {
        lock (dongPhuLock)
        {
            if (dirty)
            {
                db.DongPhus.ExecuteUpdate(s =>
                s.SetProperty(x => x.Level, Level)
                    .SetProperty(x => x.LikeCount, LikeCount)
                    .SetProperty(x => x.CurrentWateringHelpCount, CurrentWateringHelpCount)
                    .SetProperty(x => x.LastReset, LastReset)
                );
                dirty = false;
            }
            SaveHistories(db);
            SaveLikes(db);
            Garden.SaveChanges(db);
            Shop.SaveChanges(db);
            Inventory.SaveChanges(db);
        }
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
        return like.OtherCharId != CharId && !Likes.Contains(like);
    }

    public BuySeedResult BuySeed(BuySeed request, CharacterData charData)
    {
        return Shop.Buy(request, charData, Inventory);
    }
}