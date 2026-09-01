using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Data;
using MTS.Utils;

namespace MTS.Models;

public class EventInventory
{
    public int CharId { get; set; }
    public HashSet<ItemEvent> Items { get; set; } = new();
    [MemoryPackIgnore]
    [NotMapped]
    private Modify<ItemEvent> trackingItems = new();

    [MemoryPackIgnore] [NotMapped] private object lockObject = new();
    [MemoryPackIgnore]
    [NotMapped]
    private bool dirty;
    public void Add(ItemEvent item)
    {
        lock (lockObject)
        {
            item.CharId = CharId;
            if (Items.TryGetValue(item, out var owner))
            {
                owner.Count += item.Count;
                trackingItems.Update(owner);
            }
            else
            {
                Items.Add(item);
                trackingItems.Add(item);
            }
            dirty = true;
        }
    }

    public ItemEvent? GetItem(ushort id)
    {
        var item = new ItemEvent()
        {
            Id = id,
        };
        lock (lockObject)
        {
            if (Items.TryGetValue(item, out var owner))
            {
                return owner;
            }
        }
        return null;
    }
    public int CountOf(ushort id)
    {
        var item = GetItem(id);
        if (item == null)
        {
            return 0;
        }
        return item.Count;
    }
    public void Decrease(ItemEvent item, int count)
    {
        lock (lockObject)
        {
            item.Count -= count;
            if (item.Count <= 0)
            {
                Items.Remove(item);
                trackingItems.Delete(item);
            }
            else
            {
                trackingItems.Update(item);
            }
            dirty = true;
        }
    }
    public void SaveChanges(AppDbContext db)
    {
        lock (lockObject)
        {
            if (dirty)
            {
                trackingItems.Apply(db,db.ItemEvents);
                dirty = false;
            }
        }
    }
}