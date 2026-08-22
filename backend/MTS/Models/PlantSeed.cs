using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class PlantSeed
{
    public int CharId { get; set; }
    public int SlotIndex { get; set; }
    public ushort HerbId { get; set; }
    public DateTime PlantAt { get; set; } = DateTime.UtcNow;
    public DateTime HarvestableAt { get; set; }
    
    [ForeignKey(nameof(CharId))]
    public DongPhu DongPhu  { get; set; }
    
}