using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Game.DP;

public class RefCountedSemaphore
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public int RefCount;
}

public static class DongPhuManager
{
    private static ConcurrentDictionary<int, DongPhuObserver> observers = new();
    private static ConcurrentDictionary<int, DongPhuReference> references = new();
    private static ConcurrentDictionary<int, RefCountedSemaphore> creationLocks = new();
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
    public static async Task<DongPhuState> VisitDongPhu(int targetId, Client client)
    {
        await using var db = await Program.CreateDbAsync();
        return await VisitDongPhu(targetId, client, db);
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
        var gate = creationLocks.AddOrUpdate(
               charId,
               _ => new RefCountedSemaphore { RefCount = 1 },
               (_, existing) => { Interlocked.Increment(ref existing.RefCount); return existing; });
        await gate.Semaphore.WaitAsync();
        if (references.TryGetValue(charId, out var reference))
        {
            return reference;
        }
        var config = GameConfig.DongPhuConfig;
        try
        {
            var dongPhu = await db
            .DongPhus
            .AsNoTracking()
            .Include(x => x.Garden)
            .ThenInclude(x => x.Slots)
            .Include(x => x.Garden)
            .ThenInclude(x => x.Seeds)
            .Include(x => x.Shop)
            .ThenInclude(x => x.Seeds)
            .Include(x => x.Inventory)
            .ThenInclude(x => x.Seeds)
            .Include(x => x.Likes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.CharId == charId);
            var histories = await db.DongPhuHistories
                .AsNoTracking()
                .Where(x => x.CharId == charId)
                .OrderByDescending(x => x.Time)
                .Take(50)
                .ToListAsync();
            var queueHistories = new Queue<DongPhuHistory>(histories);
            dongPhu.Histories = queueHistories;
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
        finally
        {
            gate.Semaphore.Release();
            if (Interlocked.Decrement(ref gate.RefCount) == 0)
            {
                creationLocks.TryRemove(charId, out _);
            }
        }

    }

    private static void OnUserDeleted(UserData userData)
    {
        foreach (var charData in userData.CharacterDatas)
        {
            if (observers.TryRemove(charData.Id, out var observer))
            {
                observer.OnUserDeleted();
            }
            if (references.TryRemove(charData.Id, out var _))
            {

            }
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