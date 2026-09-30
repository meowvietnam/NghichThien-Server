using System.Collections.Concurrent;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.DP.Achievements;
using MTS.IAP;
using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;
using MTS.Services;
using Newtonsoft.Json;
using PayOS.Models.V2.PaymentRequests;
using Serilog;

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
        Task.Run(SaveChangesLoop);
    }

    private static async Task SaveChangesLoop()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            try
            {
                await SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SaveChangesLoop");
            }
        }
    }

    public static void OnRemoveState(DongPhuObserver observer, DongPhuState state)
    {
        if (observer.States.Count == 0)
        {
            var id = observer.DongPhu.CharId;
            observers.TryRemove(id, out var dongPhu);
        }
    }

    public static async Task OnRemoveState(DongPhuReference reference, DongPhuState state)
    {
        try
        {
            if (reference.HasNoStates())
            {
                var id = reference.DongPhu.CharId;
                var saved = await reference.SaveChanges();
                if (saved && reference.HasNoStates())
                {
                    ((ICollection<KeyValuePair<int, DongPhuReference>>)references)
                        .Remove(new KeyValuePair<int, DongPhuReference>(id, reference));
                }
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
        // Thành tựu "thăm động phủ": mỗi động phủ người khác tính tối đa 1 lần/ngày.
        referenceVisitor.DongPhu.Achievements?.AddVisit(targetId);
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

    public static async Task<string> CreateUpgradePaymentLink(DongPhu dongPhu)
    {
        var nextLevel = dongPhu.GetNextLevel();
        var amount = nextLevel.Amount;
        var paymentLink = await PaymentService.Instance.CreateLink(amount, TimeSpan.FromHours(1));
        var orderCode = paymentLink.OrderCode;
        var expireTime = DateTimeOffset
            .FromUnixTimeSeconds(paymentLink.ExpiredAt.Value)
            .UtcDateTime;
        var data = new UpgradeDongPhuData()
        {
            Level = nextLevel.Level
        };
        var order = new Order()
        {
            CharId = dongPhu.CharId,
            OrderType = OrderType.DongPhu,
            ProductId = null,
            OrderId = orderCode,
            Link = paymentLink.CheckoutUrl,
            Amount = amount,
            ExpireAt = expireTime,
            Data = JsonConvert.SerializeObject(data)
        };
        await using var db = await Program.CreateDbAsync();
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();
        return paymentLink.CheckoutUrl;
    }
    private static async Task<DongPhuReference> FindDongPhuReference(
        int charId,
        AppDbContext db,
        bool canCreate)
    {
        var gate = creationLocks.AddOrUpdate(
            charId,
            _ => new RefCountedSemaphore { RefCount = 1 },
            (_, existing) =>
            {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            });

        await gate.Semaphore.WaitAsync();

        try
        {
            if (references.TryGetValue(charId, out var reference))
            {
                return reference;
            }

            var config = GameConfig.DongPhuConfig;

            var dongPhu = await db.DongPhus
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
            if (dongPhu == null)
            {
                if (!canCreate)
                {
                    throw new InvalidException("Động phủ không tồn tại");
                }

                var charName = await db.Characters
                    .Where(x => x.Id == charId)
                    .Select(x => x.Name)
                    .FirstAsync();

                dongPhu = new DongPhu
                {
                    CharId = charId,
                    Name = charName,
                    Level = config.Levels.First().Level,
                };

                dongPhu.FirstInit(config);

                await db.DongPhus.AddAsync(dongPhu);
                await db.SaveChangesAsync();
            }

            var histories = await db.DongPhuHistories
                .AsNoTracking()
                .Where(x => x.CharId == charId)
                .OrderByDescending(x => x.Time)
                .Take(50)
                .ToListAsync();
            dongPhu.Histories = new Queue<DongPhuHistory>(histories);
            dongPhu.Init(config);
            var loaded = dongPhu;
            dongPhu.Achievements = new DongPhuAchievements(charId, () => loaded.Level);
            await dongPhu.Achievements.Load(db, dongPhu.LikeCount);
            reference = new DongPhuReference(dongPhu);
            references.TryAdd(charId, reference);
            return reference;
        }
        finally
        {
            gate.Semaphore.Release();

            if (Interlocked.Decrement(ref gate.RefCount) == 0)
            {
                ((ICollection<KeyValuePair<int, RefCountedSemaphore>>)creationLocks)
                    .Remove(new KeyValuePair<int, RefCountedSemaphore>(
                        charId,
                        gate));
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
    public static async Task Handle(Order order, AppDbContext db)
    {
        var charId = order.CharId;
        var reference = await FindDongPhuReference(charId, db, false);
        var data = JsonConvert.DeserializeObject<UpgradeDongPhuData>(order.Data);
        var nextLevel = reference.DongPhu.GetNextLevel();
  
        if (data.Level != nextLevel.Level)
        {
            throw new InvalidException($"Level không hợp lệ: {data.Level} - {nextLevel.Level}");
        }
        CharacterData charData = TcpServer.Instance
            .GetCharsOnline()
            .FirstOrDefault(x => x.Id == charId);
        if (charData == null)
        {
            charData = await db.Characters.FirstOrDefaultAsync(x => x.Id == charId);
        }
        var result = reference.DongPhu.TryUpgrade(charData, false);
        var state = reference.States.FirstOrDefault(x => x.IsMe(charId));
        if (state != null)
        {
            state.SendUpgradeResult(result);
        }
        else
        {
            if (reference.HasNoStates())
            {
                var saved = await reference.SaveChanges();
                if (saved && reference.HasNoStates())
                {
                    ((ICollection<KeyValuePair<int, DongPhuReference>>)references)
                        .Remove(new KeyValuePair<int, DongPhuReference>(reference.DongPhu.CharId, reference));
                }
            }
        }

        var purchase = new IapPurchase()
        {
            CharId = charId,
            AmountSnapshot = order.Amount,
            Store = Store.Web,
            PurchaseTokenHash = Array.Empty<byte>(),
        };
        order.Purchase = purchase;
        order.Status = OrderStatus.Complete;
        await db.IapPurchases.AddAsync(purchase);
        await db.SaveChangesAsync();
    }
}
