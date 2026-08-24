using MTS.Models;

namespace MTS.Utils;

public class PlantGardenTracking
{
    public Modify<PlantingSlot> Slots { get; set; } = new();
    public Modify<PlantSeed> Seeds { get; set; } = new();
}