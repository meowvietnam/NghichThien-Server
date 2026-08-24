using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Exceptions;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

public class DongPhuShop
{
    public int CharId { get; set; }
    public HashSet<SeedItemShop> Seeds { get; set; } = new();
    [MemoryPackIgnore] [NotMapped] private Modify<SeedItemShop> seedsTracking = new();
    [MemoryPackIgnore] [NotMapped] private bool dirty;
    [MemoryPackIgnore] [NotMapped] private object shopLock = new();

    public SeedItemShop? GetSeed(ushort herbId)
    {
        var seed = new SeedItemShop()
        {
            HerbId = herbId,
        };
        if (Seeds.TryGetValue(seed, out var ownerSeed))
        {
            return ownerSeed;
        }

        return null;
    }

    public void SetSeeds(HashSet<SeedItemShop> seeds)
    {
        lock (shopLock)
        {
            foreach (var seed in Seeds)
            {
                seedsTracking.Delete(seed);
            }
            Seeds = seeds;
            foreach (var seed in Seeds)
            {
                seedsTracking.Add(seed);
            }
            dirty = true;
        }
    }
    public void AddSeed(SeedItemShop seed)
    {
        lock (shopLock)
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

    public void DecreaseCount(SeedItemShop seed, int count)
    {
        lock (shopLock)
        {
            var remaining = seed.Count - count;
            if (remaining > 0)
            {
                seed.Count = remaining;
                seedsTracking.Update(seed);
            }
            else
            {
                Seeds.Remove(seed);
                seedsTracking.Delete(seed);
            }

            dirty = true;
        }
    }

    public BuySeedResult Buy(BuySeed request, CharacterData charData, DongPhuInventory inventory)
    {
        lock (shopLock)
        {
            var seedShopItem = GetSeed(request.HerbId);
            if (seedShopItem == null)
            {
                throw new InvalidException("Không tìm thấy hạt giống");
            }

            if (request.Count < 1)
            {
                throw new InvalidException("Số lượng không hợp lệ");
            }

            var count = request.Count;
            var price = seedShopItem.PricePerSeed * count;
            var linhThach = charData.Stats.LinhThach;
            var newLinhThach = linhThach - price;
            if (newLinhThach < 0)
            {
                throw new InvalidException("Không đủ linh thạch");
            }

            var newCount = seedShopItem.Count - count;
            if (newCount < 0)
            {
                throw new InvalidException("Số lượng vật phẩm không đủ");
            }

            var seed = new Seed()
            {
                HerbId = request.HerbId,
                Count = count,
            };
            DecreaseCount(seedShopItem, count);
            inventory.AddSeed(seed);
            charData.Stats.SetLinhThach(newLinhThach);
            return new BuySeedResult()
            {
                Success = true,
                Message = "Mua vật phẩm thành công",
                NewLinhThach = newLinhThach,
                Seed = seed,
            };
        }
    }
}