using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.DP;

public static class DongPhuManager
{
    private static ConcurrentDictionary<int, DongPhuObserver> observers = new();

    static DongPhuManager()
    {
        DbEvent.OnUserDeleted += OnUserDeleted;
    }
    public static void OnRemoveClient(DongPhuObserver observer, Client client)
    {
        if (observer.Clients.Count == 0)
        {
            var id = observer.DongPhu.CharId;
            observers.TryRemove(id, out _);
        }
    }
    public static async Task<DongPhuObserver> JoinDongPhuObserver(int charId, Client client)
    {
        if (observers.TryGetValue(charId, out var observer))
        {
            observer.AddClient(client);
            return observer;
        }
        using var db = await Program.CreateDbAsync();
        var dongPhu = await db.DongPhus.FirstOrDefaultAsync(x => x.CharId == charId);
        if (dongPhu == null)
        {
            throw new InvalidException("Động phù không tồn tại");
        }
        observer = new DongPhuObserver(dongPhu);
        observer.AddClient(client);
        observers.TryAdd(charId, observer);
        return observer;
    }
    private static void OnUserDeleted(UserData userData)
    {
        foreach (var charData in userData.CharacterDatas)
        {
            if (observers.TryRemove(charData.Id, out var observer))
            {
                observer.OnUserDeleted();
            }
        }
    }
    public static void OnDongPhuLiked(DongPhu dongPhu, DongPhuLike like)
    {
        if (observers.TryGetValue(dongPhu.CharId, out var observer))
        {
            observer.OnDongPhuLiked(like);
        }
    }
    public static void OnWateringHelp(int charId,AddWateringHelp addWateringHelp)
    {
        if (observers.TryGetValue(charId, out var observer))
        {
            observer.OnWarteringHelp(addWateringHelp);
        }
    }
    public static async Task SaveChanges()
    {
    }
}