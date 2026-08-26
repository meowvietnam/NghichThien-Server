using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game;
using MTS.Game.DP;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class PlantGarden
{
    public int CharId { get; set; }
    public HashSet<PlantingSlot> Slots { get; set; } = new();
    public HashSet<PlantSeed> Seeds { get; set; } = new();
    [MemoryPackIgnore][NotMapped] private object gardenLock = new();
    [MemoryPackIgnore][NotMapped] private PlantGardenTracking tracking = new();

    [MemoryPackIgnore][NotMapped] private DongPhuConfig config;
    [MemoryPackIgnore][NotMapped] private DongPhuLevel level;

    public void SetConfig(DongPhuConfig config)
    {
        this.config = config;
    }

    public void InitLevel(DongPhuLevel level)
    {
        this.level = level;
    }
    public void SetLevel(DongPhuLevel level)
    {
        this.level = level;
        foreach (var slot in Slots)
        {
            var unlock = config.IsLevelGreaterOrEqual(level, slot.UnlockLevel);
            if (unlock != slot.IsLocked)
            {
                slot.IsLocked = unlock;
                tracking.Slots.Update(slot);
            }
        }
    }
    public void FirstInit()
    {
        var slots = new HashSet<PlantingSlot>(9);
        for (ushort i = 0; i < 9; i++)
        {
            var unlockLevel = (DongPhuLevel)((i / 3) + 1);
            var slot = new PlantingSlot()
            {
                CharId = CharId,
                UnlockLevel = unlockLevel,
                SlotIndex = i
            };
            slots.Add(slot);
        }
        Slots = slots;
        SetLevel(level);
    }
    private DongPhuLevelConfig GetLevelConfig()
    {
        return config.Levels.First(x => x.Level == level);
    }

    public (PlantingSlot, TimeSpan reduceTime) TryWatering(WateringHelpRequest request, CharacterData helper)
    {
        lock (gardenLock)
        {
            var slotIndex = request.SlotIndex;
            var slot = GetActiveSlot(slotIndex);
            var plantSeed = GetActiveSeed(slotIndex);
            var wateringHelp = new WateringHelp()
            {
                CharId = helper.Id,
                Name = helper.Name,
            };
            var levelConfig = GetLevelConfig();
            var reduceTime = levelConfig.ReduceTime;
            plantSeed.TryWatered(wateringHelp, reduceTime, levelConfig.SlotMaxWaterHelpPerDay);
            tracking.Seeds.Update(plantSeed);
            var addWateringHelp = new AddWateringHelp()
            {
                SlotIndex = slotIndex,
                WateringHelp = wateringHelp,
            };
            return (slot, reduceTime);
        }
    }

    public (PlantSeed plantSeed, PlantingSlot slot) Plant(Seed seed, ushort slotIndex, CharacterData charData)
    {
        lock (gardenLock)
        {
            if (CharId != charData.Id)
            {
                throw new InvalidException("Không thể trồng cho người khác");
            }

            var slot = GetActiveSlot(slotIndex);
            if (!IsSlotEmpty(slotIndex))
            {
                throw new InvalidException("Ô đất này đã được trồng cây");
            }

            var herb = GameConfig.LuyenDanResources.HerbDatas.FirstOrDefault(x => x.Id == seed.HerbId);
            if (herb == null)
            {
                throw new InvalidException("Hạt giống này không tồn tại");
            }
            var tier = herb.Tier;
            var growthDuration = config.Seeds.First(x => x.Tier == tier).GrowthDuration;
            var plantSeed = new PlantSeed(CharId, slotIndex, seed.HerbId, growthDuration);
            Seeds.Add(plantSeed);
            tracking.Seeds.Add(plantSeed);
            return (plantSeed, slot);
        }
    }
    public (List<IDongPhuReward> rewards, PlantingSlot slot) TryHarvest(HarvestRequest request, CharacterData charData)
    {
        lock (gardenLock)
        {
            var slotIndex = request.SlotIndex;
            var slot = GetActiveSlot(slotIndex);
            var plantSeed = GetActiveSeed(slotIndex);
            var rewards = plantSeed.TryHarvest(charData);
            Seeds.Remove(plantSeed);
            tracking.Seeds.Delete(plantSeed);
            return (rewards, slot);
        }
    }
    public void ResetDaily()
    {
        lock (gardenLock)
        {
            foreach (var plantSeed in Seeds)
            {
                if (plantSeed.ResetDaily())
                {
                    tracking.Seeds.Update(plantSeed);
                }
            }
        }
    }
    public bool IsSlotEmpty(ushort slotIndex)
    {
        var plantSeed = new PlantSeed()
        {
            SlotIndex = slotIndex
        };
        return !Seeds.Contains(plantSeed);
    }

    public PlantSeed GetActiveSeed(ushort slotIndex)
    {
        var plantSeed = new PlantSeed()
        {
            SlotIndex = slotIndex
        };
        if (Seeds.TryGetValue(plantSeed, out var seed))
        {
            return seed;
        }

        throw new InvalidException("Ô đất này chưa được trồng cây");
    }

    private PlantingSlot GetActiveSlot(ushort slotIndex)
    {
        var slot = Slots.FirstOrDefault(x => x.SlotIndex == slotIndex);
        if (slot == null)
        {
            throw new InvalidException("Ô đất không tồn tại");
        }
        if (slot.IsLocked)
        {
            throw new InvalidException("Ô đất chưa mở");
        }

        return slot;
    }

    public void SaveChanges(AppDbContext db)
    {
        lock (gardenLock)
        {
            tracking.Slots.Apply(db, db.PlantingSlots);
            tracking.Seeds.Apply(db, db.PlantSeeds);
            tracking.Clear();
        }
    }
}