using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Game.DP;

public static class DongPhuManager
{
    private static ConcurrentDictionary<int, DongPhuObserver> observers = new();
    private static ConcurrentDictionary<int, DongPhuReference> references = new();

    static DongPhuManager()
    {
        DbEvent.OnUserDeleted += OnUserDeleted;
    }

    public static void OnRemoveState(DongPhuObserver observer, DongPhuState state)
    {
        if (observer.States.Count == 0)
        {
            var id = observer.DongPhu.CharId;
            observers.TryRemove(id, out _);
        }
    }

    public static async Task OnRemoveState(DongPhuReference reference, DongPhuState state)
    {
        try
        {
            var cts = reference.GetCancelToken();
            await cts.CancelAsync();
            cts = reference.ResetCancelToken();
            await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);
            if (reference.States.Count == 0)
            {
                var id = reference.DongPhu.CharId;
                references.TryRemove(id, out _);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public static async Task<DongPhuState> VisitDongPhu(string name, Client client)
    {
        await using var db = await Program.CreateDbAsync();
        var charId = await db.DongPhus
            .Where(x => x.Name == name)
            .Select(x => x.CharId)
            .FirstOrDefaultAsync();
        if (charId == 0)
        {
            throw new InvalidException("Động phủ không tồn tại");
        }
        return await VisitDongPhu(charId, client, db);
    }

    public static async Task<DongPhuState>
        VisitDongPhu(int targetId, Client client, AppDbContext db)
    {
        var charData = client.CharacterData;
        if (charData.Progression.MaxLevel < 4)
        {
            throw new InvalidException("Yêu cầu đạt Trúc cơ sơ kì");
        }

        var visitorId = charData.Id;
        var (observer, referenceTarget, referenceVisitor) =
            await FindOrCreateDongPhuObserverAndReference(targetId, visitorId, db);
        var state = new DongPhuState(client, observer, referenceTarget, referenceVisitor);
        referenceTarget.AddState(state);
        referenceVisitor.AddState(state);
        observer.AddState(state);
        return state;
    }

    private static async Task<(DongPhuObserver observer, DongPhuReference referenceTarget, DongPhuReference
            referenceVisitor)>
        FindOrCreateDongPhuObserverAndReference(int targetId, int visitorCharId, AppDbContext db)
    {
        var canCreate = targetId == visitorCharId;
        var referenceTarget = await FindDongPhuReference(targetId, db, canCreate);
        var referenceVisitor = await FindDongPhuReference(visitorCharId, db, canCreate);
        if (observers.TryGetValue(targetId, out var observer))
        {
            return (observer, referenceTarget, referenceVisitor);
        }

        observer = new DongPhuObserver(referenceTarget.DongPhu);
        observers.TryAdd(targetId, observer);
        return (observer, referenceTarget, referenceVisitor);
    }

    private static async Task<DongPhuReference> FindDongPhuReference(int charId, AppDbContext db, bool canCreate)
    {
        if (references.TryGetValue(charId, out var reference))
        {
            return reference;
        }

        var config = GameConfig.DongPhuConfig;
        var dongPhu = await db
            .DongPhus
            .FirstOrDefaultAsync(x => x.CharId == charId);
        if (dongPhu == null)
        {
            if (!canCreate)
            {
                throw new InvalidException("Động phủ không tồn tại");
            }

            var charName = await db.Characters.Where(x => x.Id == charId).Select(x => x.Name).FirstAsync();
            dongPhu = new DongPhu()
            {
                CharId = charId,
                Name = charName,
                Level = config.Levels.First().Level,
            };
            dongPhu.FirstInit(config);
            await db.DongPhus.AddAsync(dongPhu);
            await db.SaveChangesAsync();
        }

        dongPhu.Init(config);
        reference = new DongPhuReference(dongPhu);
        references.TryAdd(charId, reference);
        return reference;
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

    public static void OnWateringHelp(int charId, AddWateringHelp addWateringHelp)
    {
        if (observers.TryGetValue(charId, out var observer))
        {
            observer.OnWateringHelp(addWateringHelp);
        }
    }

    public static void OnResetDaily(DongPhu dongPhu)
    {
        if (observers.TryGetValue(dongPhu.CharId, out var obs))
        {
            obs.OnMyResetDaily(dongPhu);
        }
    }

    public static async Task SaveChanges()
    {
        foreach (var reference in references.Values)
        {
            await reference.SaveChanges();
        }
    }
}