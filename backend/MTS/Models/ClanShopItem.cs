using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class ClanShopItem
{
    [Key]
    public int Id { get; set; }
    public int ClanId { get; set; }
    public int OwnerCharId { get; set; }
    [MemoryPackIgnore] public ushort ItemId { get; set; }
    [MemoryPackIgnore] public ItemType ItemType { get; set; }
    [MemoryPackIgnore] public int Count { get; set; }
    public sbyte ItemDurability { get; set; }
    public long Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public ItemRef Item
    {
        get => new(ItemId, ItemType, Count);
        set
        {
            ItemId = value.Id;
            ItemType = value.Type;
            Count = value.Count;
        }
    }
}
