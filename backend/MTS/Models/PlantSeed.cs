using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Exceptions;

namespace MTS.Models;

public class PlantSeed: IEquatable<PlantSeed>
{
    public int CharId { get; set; }
    public ushort SlotIndex { get; set; }
    public ushort HerbId { get; set; }
    public DateTime PlantAt { get; set; }
    public DateTime EstimateHarvestAt { get; set; }
    public TimeSpan TimeUntilHarvest { get; set; }
    public DateTime LastInit { get; set; }
    public bool IsHavest { get; set; }
    public ushort MaxWaterHelpPerDay { get; set; }
    public TimeSpan WateringTimeReduction { get; set; }
    public List<WateringHelp> WateringHelps { get; set; } = new();
    [MemoryPackConstructor]
    public PlantSeed()
    {
    }

    public PlantSeed(int charId, ushort slotIndex, ushort herbId, TimeSpan timeUntilHarvest, ushort maxWaterHelpPerDay,
        TimeSpan wateringTimeReduction)
    {
        CharId = charId;
        SlotIndex = slotIndex;
        HerbId = herbId;
        PlantAt = Now();
        TimeUntilHarvest = timeUntilHarvest;
        EstimateHarvestAt = PlantAt.Add(timeUntilHarvest);
        MaxWaterHelpPerDay = maxWaterHelpPerDay;
        WateringTimeReduction = wateringTimeReduction;
        Init();
    }

    public void Init()
    {
        if (Utils.Utils.IsToday(LastInit))
        {
            return;
        }
        LastInit = Now();
        WateringHelps.Clear();
    }

    private DateTime Now()
    {
        return DateTime.UtcNow;
    }

    public bool CanHarvest()
    {
        return Now() >= EstimateHarvestAt;
    }

    public void CanWatering(int charId)
    {
        if (CharId == charId)
        {
            throw new InvalidException("Không thể tự tưới cây");
        }
        if (WateringHelps.Count >= MaxWaterHelpPerDay)
        {
            throw new InvalidException("Cây này đã đạt giới hạn tưới mỗi ngày");
        }

        if (WateringHelps.Any(x => x.CharId == charId))
        {
            throw new InvalidException("Hôm nay bạn đã tưới cây này");
        }
    }

    public void TryWatered(WateringHelp wateringHelp)
    {
        CanWatering(wateringHelp.CharId);
        WateringHelps.Add(wateringHelp);
        EstimateHarvestAt -= WateringTimeReduction;
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