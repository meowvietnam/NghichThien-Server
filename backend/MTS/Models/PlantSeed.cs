using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class PlantSeed
{
    public int CharId { get; set; }
    public int SlotIndex { get; set; }
    public ushort HerbId { get; set; }
    public DateTime PlantAt { get; set; }
    public TimeSpan TimeUntilHarvest { get; set; }
    public DateTime LastWateredAt { get; set; }
    public int WateredCount { get; set; }
    [ForeignKey(nameof(CharId))]
    public DongPhu DongPhu  { get; set; }
    public PlantSeed()
    {
        PlantAt = DateTime.UtcNow;
        LastWateredAt = PlantAt;
        WateredCount = 0;
    }

    public bool NeedWatered()
    {
        return false;
    }
}