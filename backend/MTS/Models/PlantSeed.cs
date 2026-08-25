using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Exceptions;

namespace MTS.Models;

public class PlantSeed : IEquatable<PlantSeed>
{
    public int CharId { get; set; }
    public ushort SlotIndex { get; set; }
    public ushort HerbId { get; set; }
    public DateTime PlantAt { get; set; }
    public DateTime EstimateHarvestAt { get; set; }
    public bool IsHavest { get; set; }
    public List<WateringHelp> WateringHelps { get; set; } = new();

    [MemoryPackConstructor]
    public PlantSeed()
    {
    }

    public PlantSeed(int charId, ushort slotIndex, ushort herbId, TimeSpan growthDuration)
    {
        CharId = charId;
        SlotIndex = slotIndex;
        HerbId = herbId;
        PlantAt = Now();
        EstimateHarvestAt = PlantAt.Add(growthDuration);
        ResetDaily();
    }

    public bool ResetDaily()
    {
        if (WateringHelps.Count == 0)
        {
            return false;
        }
        WateringHelps.Clear();
        return true;
    }

    private DateTime Now()
    {
        return DateTime.UtcNow;
    }

    public bool CanHarvest()
    {
        return Now() >= EstimateHarvestAt;
    }

    public void CanWatering(int charId, int maxWaterHelpPerDay)
    {
        if (CharId == charId)
        {
            throw new InvalidException("Không thể tự tưới cây");
        }

        if (WateringHelps.Count >= maxWaterHelpPerDay)
        {
            throw new InvalidException("Cây này đã đạt giới hạn tưới mỗi ngày");
        }

        if (WateringHelps.Any(x => x.CharId == charId))
        {
            throw new InvalidException("Hôm nay bạn đã tưới cây này");
        }
    }

    public void TryWatered(WateringHelp wateringHelp,TimeSpan wateringTimeReduction, int maxWaterHelpPerDay)
    {
        if (CanHarvest())
        {
            throw new InvalidException("Cây đã có thể thu hoạch");
        }
        CanWatering(wateringHelp.CharId, maxWaterHelpPerDay);
        WateringHelps.Add(wateringHelp);
        EstimateHarvestAt -= wateringTimeReduction;
    }

    public ItemRef TryHarvest(CharacterData charData)
    {
        if (charData.Id != CharId)
        {
            throw new InvalidException("Bạn không thể thu hoạch ô đất của người khác");
        }

        if (IsHavest)
        {
            throw new InvalidException("Ô đất đã được thu hoạch");
        }

        if (!CanHarvest())
        {
            throw new InvalidException("Chưa thể thu hoạch");
        }

        IsHavest = true;
        var itemRef = new ItemRef()
        {
            CharId = charData.Id,
            Type = ItemType.Herb,
            Id = HerbId,
            Count = 1,
        };
        charData.InventoryManager.Add(itemRef);
        return itemRef;
    }

    public bool Equals(PlantSeed? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SlotIndex == other.SlotIndex;
    }

    public override int GetHashCode()
    {
        return SlotIndex;
    }
}