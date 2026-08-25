using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Exceptions;
using MTS.Game.DP;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

public class DongPhu
{
    public int CharId { get; set; }
    public int LikeCount { get; set; }
    public string Name { get; set; }
    public DongPhuLevel Level { get; set; }
    public PlantGarden Garden { get; set; } = new();
    [MemoryPackIgnore] public DongPhuShop Shop { get; set; } = new();
    [MemoryPackIgnore] public DongPhuInventory Inventory { get; set; } = new();
    [MemoryPackIgnore] public HashSet<DongPhuLike> Likes { get; set; }
    [MemoryPackIgnore] [NotMapped] public Queue<DongPhuHistory> Histories { get; set; } = new();
    [MemoryPackIgnore] [NotMapped] private List<DongPhuHistory> addedHistories = new();
    [MemoryPackIgnore] [NotMapped] private bool dirty;
    [MemoryPackIgnore] [NotMapped] private Modify<DongPhuLike> likesTracking = new();
    private object dongPhuLock = new();

    public void AddLike(DongPhuLike like)
    {
        lock (dongPhuLock)
        {
            if (!CanLike(like))
            {
                throw new InvalidException("Không thể thích Động phủ người chơi này");
            }

            Likes.Add(like);
            likesTracking.Add(like);
            LikeCount++;
            dirty = true;
            DongPhuManager.OnDongPhuLiked(this, like);
        }
    }

    public void TryHarvest(HarvestRequest request, CharacterData charData)
    {
        Garden.TryHarvest(request, charData);
    }

    public void TryWatering(WateringHelpRequest request, CharacterData helper)
    {
        Garden.TryWatering(request, helper);
    }

    public void AddHistory(string log)
    {
        var history = new DongPhuHistory()
        {
            CharId = CharId,
            Log = log,
        };
        lock (dongPhuLock)
        {
            addedHistories.Add(history);
            Histories.Enqueue(history);
            if (Histories.Count > 50)
            {
                Histories.Dequeue();
            }
        }
    }

    public bool CanLike(int charId)
    {
        var like = new DongPhuLike()
        {
            OwnerCharId = CharId,
            OtherCharId = charId,
        };
        return CanLike(like);
    }

    private bool CanLike(DongPhuLike like)
    {
        return like.OtherCharId != CharId && Likes.Contains(like);
    }
}