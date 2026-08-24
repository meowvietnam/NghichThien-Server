using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

public class PlantGarden
{
    public int CharId { get; set; }
    public HashSet<PlantingSlot> Slots { get; set; } = new();
    public HashSet<PlantSeed> Seeds { get; set; } = new();
    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public DongPhu DongPhu  { get; set; }
    [MemoryPackIgnore]
    [NotMapped]
    private object gardenLock = new();
    [MemoryPackIgnore]
    [NotMapped]
    private PlantGardenTracking tracking = new();
    public void TryWatering(WateringHelpRequest request, CharacterData helper)
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
            plantSeed.TryWatered(wateringHelp);
            tracking.Seeds.Update(plantSeed);
            var addWateringHelp = new AddWateringHelp()
            {
                SlotIndex = slotIndex,
                WateringHelp = wateringHelp,
            };
            DongPhuManager.OnWateringHelp(CharId,addWateringHelp);
        }
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
}