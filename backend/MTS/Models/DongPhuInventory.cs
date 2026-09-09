using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Data;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class DongPhuInventory
{
    public int CharId { get; set; }
    public HashSet<Seed> Seeds { get; set; } = new();
    [MemoryPackIgnore] [NotMapped] private Modify<Seed> seedsTracking = new();
    [MemoryPackIgnore] [NotMapped] private bool dirty;
    [MemoryPackIgnore] [NotMapped] private object inventoryLock = new();
    public Seed? GetSeed(ushort herbId)
    {
        var seed = new Seed()
        {
            HerbId = herbId,
        };
        if (Seeds.TryGetValue(seed, out var ownerSeed))
        {
            return ownerSeed;
        }
        return null;
    }

    public void AddSeed(Seed seed)
    {
        lock (inventoryLock)
        {
            seed.CharId = CharId;
            if (Seeds.TryGetValue(seed, out var ownerSeed))
            {
                ownerSeed.Count += seed.Count;
                seedsTracking.Update(ownerSeed);
            }
            else
            {
                Seeds.Add(seed);
                seedsTracking.Add(seed);
            }
            dirty = true;
        }
    }

    public void DecreaseCount(Seed seed, int count)
    {
        lock (inventoryLock)
        {
            var remaining = seed.Count - count;
            if (remaining > 0)
            {
                seed.Count = remaining;
                seedsTracking.Update(seed);
            }
            else
            {
                seed.Count = 0;
                Seeds.Remove(seed);
                seedsTracking.Delete(seed);
            }
            dirty = true;
        }
    }
    public void SaveChanges(AppDbContext db)
    {
        lock(inventoryLock)
        {
            if(!dirty)
            {
                return;
            }
            seedsTracking.Apply(db, db.Seeds);
        }
    }
    public void AcceptChanges()
    {
        lock(inventoryLock)
        {
            seedsTracking.Clear();
            dirty = false;
        }
    }
}
