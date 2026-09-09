using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using MTS.Data;
using MTS.Models;
using MTS.Services.Clans;
using Network.Models.Clans;
using Xunit;

namespace MTS.Tests.Clans;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MTS_CLAN_TEST_CONNECTION")))
            Skip = "Set MTS_CLAN_TEST_CONNECTION to run disposable MySQL integration tests.";
    }
}

public sealed class ClanDatabase : IDisposable, IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> options;
    public ClanDatabase()
    {
        // Opt-in only. Never reads appsettings or connects to an application database.
        var connection = Environment.GetEnvironmentVariable("MTS_CLAN_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set MTS_CLAN_TEST_CONNECTION to a disposable MySQL database named mts_clan_test_*.");
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(connection);
        if (!builder.Database.StartsWith("mts_clan_test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Only disposable mts_clan_test_* databases are allowed.");
        options = new DbContextOptionsBuilder<AppDbContext>().UseMySql(connection,
            new MySqlServerVersion(new Version(8, 0, 36)), x => x.EnableRetryOnFailure()).Options;
        using var db = CreateDbContext();
        if (db.Database.CanConnect()) throw new InvalidOperationException("Test database already exists. Use a fresh database name.");
        db.Database.Migrate();
    }
    public AppDbContext CreateDbContext() => new(options);
    public Actor NewActor(int money = 1000)
    {
        using var db = CreateDbContext();
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var user = new UserData();
        db.Entry(user).State = EntityState.Added;
        db.SaveChanges();
        var character = new CharacterData { UserId = user.Id, Name = "T" + Guid.NewGuid().ToString("N") };
        db.Entry(character).State = EntityState.Added;
        db.SaveChanges();
        var stats = new CharacterStats { CharId = character.Id };
        stats.SetLinhThach(money);
        db.Entry(stats).State = EntityState.Added;
        var progression = new Progression { CharId = character.Id };
        db.Entry(progression).State = EntityState.Added;
        db.SaveChanges();
        return new Actor(character.Id, money);
    }
    public ClanService Service(int capacity = 30, int dailyLimit = 100000, int creationCost = 0) =>
        new(this, Options.Create(new ClanOptions { InitialCapacity = capacity, DailyDonationLimit = dailyLimit, CreationCost = creationCost }));
    public void Dispose() { using var db = CreateDbContext(); db.Database.EnsureDeleted(); }
}

public sealed class Actor(int id, int money) : IClanActor
{
    public object SyncRoot { get; } = new();
    public int CharId => id;
    public int Level => 0;
    public int LinhThach { get; set; } = money;
}

public class ClanIntegrationTests(ClanDatabase database) : IClassFixture<ClanDatabase>
{
    private static ClanCommandRequest Command(ClanCommandType type, int clanId = 0, int target = 0) =>
        new() { RequestId = Guid.NewGuid(), Command = type, ClanId = clanId, TargetCharId = target };
    private static ClanResponse Run(ClanService service, Actor actor, ClanCommandRequest request)
    {
        var result = service.Execute(actor, request);
        Assert.True(result.Success, result.Code);
        return result;
    }
    private static int Create(ClanService service, Actor actor, bool open = false)
    {
        var request = Command(ClanCommandType.Create);
        request.Name = "Tông " + Guid.NewGuid().ToString("N");
        request.JoinPolicy = open ? 0 : 1;
        return Run(service, actor, request).ClanId;
    }
    private static ClanResponse Query(ClanService service, Actor actor, ClanQueryType type, int clanId = 0, long cursor = 0) =>
        service.Query(actor.CharId, new ClanQueryRequest { RequestId = Guid.NewGuid(), Query = type, ClanId = clanId, AfterId = cursor });

    [MySqlFact]
    public void Create_has_membership_receipt_audit_and_outbox()
    {
        var actor = database.NewActor(); var service = database.Service(); var id = Create(service, actor);
        using var db = database.CreateDbContext();
        Assert.Equal(actor.CharId, db.Clans.Single(x => x.Id == id).LeaderCharId);
        Assert.Equal(id, db.ClanMembers.Single(x => x.CharId == actor.CharId).ClanId);
        Assert.Single(db.ClanCommandReceipts.Where(x => x.ActorCharId == actor.CharId));
        Assert.Single(db.ClanAuditLogs.Where(x => x.ClanId == id));
        Assert.Single(db.ClanOutboxEvents.Where(x => x.ClanId == id && x.PublishedAt == null));
    }

    [MySqlFact]
    public void Duplicate_donation_debits_once_and_does_not_restore_old_wallet_on_replay()
    {
        var actor = database.NewActor(); var service = database.Service(); var id = Create(service, actor);
        var request = Command(ClanCommandType.Donate, id); request.Amount = 100;
        Run(service, actor, request); Assert.Equal(900, actor.LinhThach);
        actor.LinhThach = 850; // a later gameplay purchase
        var replay = Run(service, actor, request); Assert.Equal(850, replay.LinhThach);
        using var db = database.CreateDbContext();
        Assert.Equal(100, db.Clans.Single(x => x.Id == id).Funds);
        Assert.Equal(900, db.CharacterStats.Single(x => x.CharId == actor.CharId).LinhThach);
        Assert.Equal(3, db.ClanLedgerEntries.Count(x => x.ClanId == id));
        request.Amount = 200;
        Assert.Equal("RequestIdReused", service.Execute(actor, request).Code);
    }

    [MySqlFact]
    public void Failed_donation_rolls_back_everything()
    {
        var actor = database.NewActor(50); var service = database.Service(); var id = Create(service, actor);
        var request = Command(ClanCommandType.Donate, id); request.Amount = 100;
        Assert.Equal("InsufficientFunds", service.Execute(actor, request).Code);
        using var db = database.CreateDbContext();
        Assert.Equal(0, db.Clans.Single(x => x.Id == id).Funds);
        Assert.Equal(50, actor.LinhThach);
        Assert.Empty(db.ClanLedgerEntries.Where(x => x.ClanId == id));
        Assert.Empty(db.ClanMemberPeriodStats.Where(x => x.CharId == actor.CharId));
        Assert.DoesNotContain(db.ClanCommandReceipts, x => x.RequestId == request.RequestId);
    }

    [MySqlFact]
    public void Daily_limit_is_character_wide()
    {
        var actor = database.NewActor(); var service = database.Service(dailyLimit: 100); var id = Create(service, actor);
        var request = Command(ClanCommandType.Donate, id); request.Amount = 100; Run(service, actor, request);
        request = Command(ClanCommandType.Donate, id); request.Amount = 1;
        Assert.Equal("DailyLimit", service.Execute(actor, request).Code);
        using var db = database.CreateDbContext();
        Assert.Equal(0, db.ClanMemberPeriodStats.Single(x => x.CharId == actor.CharId).ScopeId);
    }

    [MySqlFact]
    public void Approval_permissions_transfer_and_kick_revoke_access()
    {
        var leader = database.NewActor(); var recruit = database.NewActor(); var service = database.Service();
        var id = Create(service, leader);
        Run(service, recruit, Command(ClanCommandType.Apply, id));
        Assert.Equal("NotMember", Query(service, recruit, ClanQueryType.Applications, id).Code);
        var approve = Command(ClanCommandType.ReviewApplication, id, recruit.CharId); approve.Approve = true;
        Run(service, leader, approve);
        Assert.Equal("Forbidden", Query(service, recruit, ClanQueryType.Applications, id).Code);
        Assert.Equal("TransferLeadershipFirst", service.Execute(leader, Command(ClanCommandType.Leave, id)).Code);
        Run(service, leader, Command(ClanCommandType.TransferLeadership, id, recruit.CharId));
        Run(service, recruit, Command(ClanCommandType.Kick, id, leader.CharId));
        Assert.Equal("NotMember", Query(service, leader, ClanQueryType.Members, id).Code);
        Assert.Equal("JoinCooldown", service.Execute(leader, Command(ClanCommandType.Apply, id)).Code);
    }

    [MySqlFact]
    public async Task Two_recruits_cannot_take_the_last_slot()
    {
        var leader = database.NewActor(); var one = database.NewActor(); var two = database.NewActor();
        var service = database.Service(capacity: 2); var id = Create(service, leader, true);
        using var start = new Barrier(2);
        var results = await Task.WhenAll(new[] { one, two }.Select(a => Task.Run(() => {
            start.SignalAndWait(); return service.Execute(a, Command(ClanCommandType.Apply, id));
        })));
        Assert.Single(results, x => x.Success);
        using var db = database.CreateDbContext();
        Assert.Equal(2, db.ClanMembers.Count(x => x.ClanId == id));
        Assert.Equal(2, db.Clans.Single(x => x.Id == id).MemberCount);
    }

    [MySqlFact]
    public async Task Two_clans_cannot_approve_the_same_character()
    {
        var one = database.NewActor(); var two = database.NewActor(); var recruit = database.NewActor();
        var service = database.Service(); var a = Create(service, one); var b = Create(service, two);
        Run(service, recruit, Command(ClanCommandType.Apply, a)); Run(service, recruit, Command(ClanCommandType.Apply, b));
        using var start = new Barrier(2);
        var results = await Task.WhenAll(new[] { (one, a), (two, b) }.Select(pair => Task.Run(() => {
            var request = Command(ClanCommandType.ReviewApplication, pair.Item2, recruit.CharId); request.Approve = true;
            start.SignalAndWait(); return service.Execute(pair.Item1, request);
        })));
        Assert.Single(results, x => x.Success);
        using var db = database.CreateDbContext();
        Assert.Single(db.ClanMembers.Where(x => x.CharId == recruit.CharId));
        Assert.Equal(3, db.Clans.Where(x => x.Id == a || x.Id == b).Sum(x => x.MemberCount));
    }

    [MySqlFact]
    public void Public_details_do_not_leak_announcement_or_funds()
    {
        var leader = database.NewActor(); var outsider = database.NewActor(); var service = database.Service(); var id = Create(service, leader);
        var update = Command(ClanCommandType.UpdateSettings, id); update.Announcement = "Secret"; Run(service, leader, update);
        var donate = Command(ClanCommandType.Donate, id); donate.Amount = 10; Run(service, leader, donate);
        var publicDetail = Query(service, outsider, ClanQueryType.Detail, id).Detail;
        Assert.Equal("", publicDetail.Announcement); Assert.Equal(0, publicDetail.Funds); Assert.Equal(-1, publicDetail.MyRole);
        Assert.Equal("Secret", Query(service, leader, ClanQueryType.Mine).Detail.Announcement);
    }

    [MySqlFact]
    public void Prefix_search_normalizes_and_escapes_wildcards()
    {
        var actor = database.NewActor(); var service = database.Service();
        var request = Command(ClanCommandType.Create); request.Name = "  Thanh   Vân " + Guid.NewGuid().ToString("N");
        var id = Run(service, actor, request).ClanId;
        var results = service.Query(actor.CharId, new ClanQueryRequest { RequestId = Guid.NewGuid(), Query = ClanQueryType.Search, Search = "thanh vân" });
        Assert.Contains(results.Clans, x => x.Id == id);
        results = service.Query(actor.CharId, new ClanQueryRequest { RequestId = Guid.NewGuid(), Query = ClanQueryType.Search, Search = "%%" });
        Assert.Empty(results.Clans);
    }

    [MySqlFact]
    public void Stale_version_does_not_overwrite_settings()
    {
        var actor = database.NewActor(); var service = database.Service(); var id = Create(service, actor);
        var first = Command(ClanCommandType.UpdateSettings, id); first.ExpectedVersion = 1; Run(service, actor, first);
        var second = Command(ClanCommandType.UpdateSettings, id); second.ExpectedVersion = 1;
        Assert.Equal("StaleVersion", service.Execute(actor, second).Code);
    }

    [MySqlFact]
    public void Creation_fee_and_insufficient_fee_are_atomic()
    {
        var actor = database.NewActor(100); var service = database.Service(creationCost: 60); var id = Create(service, actor);
        Assert.Equal(40, actor.LinhThach);
        var poor = database.NewActor(10); var request = Command(ClanCommandType.Create); request.Name = "Poor" + Guid.NewGuid().ToString("N");
        Assert.Equal("InsufficientFunds", service.Execute(poor, request).Code);
        using var db = database.CreateDbContext();
        Assert.False(db.Clans.Any(x => x.Name == request.Name));
        Assert.False(db.ClanMembers.Any(x => x.CharId == poor.CharId));
        Assert.Equal(40, db.CharacterStats.Single(x => x.CharId == actor.CharId).LinhThach);
    }

    [MySqlFact]
    public void Dissolve_keeps_history_and_hides_from_search()
    {
        var actor = database.NewActor(); var service = database.Service(); var id = Create(service, actor);
        Run(service, actor, Command(ClanCommandType.Dissolve, id));
        Assert.Null(Query(service, actor, ClanQueryType.Mine).Detail);
        using var db = database.CreateDbContext();
        Assert.Equal(ClanStatus.Dissolved, db.Clans.Single(x => x.Id == id).Status);
        Assert.Equal(2, db.ClanAuditLogs.Count(x => x.ClanId == id));
        Assert.Equal(0, db.ClanMembers.Count(x => x.ClanId == id));
    }

    [MySqlFact]
    public void Donation_uses_live_stats_and_later_autosave_preserves_committed_balance()
    {
        var actor = database.NewActor(); var service = database.Service(); var id = Create(service, actor);
        CharacterStats stats;
        using (var db = database.CreateDbContext()) stats = db.CharacterStats.AsNoTracking().Single(x => x.CharId == actor.CharId);
        stats.SetLinhThach(800); // gameplay change not yet flushed by periodic autosave
        var live = new StatsActor(stats);
        var request = Command(ClanCommandType.Donate, id); request.Amount = 100;
        Assert.True(service.Execute(live, request).Success);
        Assert.Equal(700, stats.LinhThach);
        lock (live.SyncRoot)
        {
            using var autosave = database.CreateDbContext();
            stats.SaveChanges(autosave).GetAwaiter().GetResult();
        }
        using var verify = database.CreateDbContext();
        Assert.Equal(700, verify.CharacterStats.Single(x => x.CharId == actor.CharId).LinhThach);
        Assert.Equal(100, verify.Clans.Single(x => x.Id == id).Funds);
    }

    private sealed class StatsActor(CharacterStats stats) : IClanActor
    {
        public object SyncRoot { get; } = new();
        public int CharId => stats.CharId;
        public int Level => 0;
        public int LinhThach { get => stats.LinhThach; set => stats.SetLinhThach(value); }
    }

    [MySqlFact]
    public void Lost_commit_ack_recovers_receipt_and_updates_live_balance()
    {
        var actor = database.NewActor(); var normal = database.Service(); var id = Create(normal, actor);
        var interceptor = new LostCommitAck();
        var service = new ClanService(new FaultFactory(database, interceptor), Options.Create(new ClanOptions()));
        var request = Command(ClanCommandType.Donate, id); request.Amount = 100;
        Run(service, actor, request);
        Assert.Equal(900, actor.LinhThach);
        using var db = database.CreateDbContext();
        Assert.Equal(100, db.Clans.Single(x => x.Id == id).Funds);
        Assert.Equal(900, db.CharacterStats.Single(x => x.CharId == actor.CharId).LinhThach);
        Assert.Single(db.ClanCommandReceipts.Where(x => x.RequestId == request.RequestId));
    }

    [MySqlFact]
    public void Failure_after_wallet_update_rolls_back_money_and_clan()
    {
        var actor = database.NewActor(); var normal = database.Service(); var id = Create(normal, actor);
        var service = new ClanService(new FaultFactory(database, new FailReceiptSave()), Options.Create(new ClanOptions()));
        var request = Command(ClanCommandType.Donate, id); request.Amount = 100;
        Assert.Throws<InvalidOperationException>(() => service.Execute(actor, request));
        using var db = database.CreateDbContext();
        Assert.Equal(1000, actor.LinhThach);
        Assert.Equal(1000, db.CharacterStats.Single(x => x.CharId == actor.CharId).LinhThach);
        Assert.Equal(0, db.Clans.Single(x => x.Id == id).Funds);
        Assert.Empty(db.ClanLedgerEntries.Where(x => x.ClanId == id));
    }

    [MySqlFact]
    public void Reject_cancel_and_expired_applications_cannot_be_approved()
    {
        var leader = database.NewActor(); var applicant = database.NewActor(); var service = database.Service();
        var id = Create(service, leader);
        Run(service, applicant, Command(ClanCommandType.Apply, id));
        Run(service, applicant, Command(ClanCommandType.CancelApplication, id));
        var review = Command(ClanCommandType.ReviewApplication, id, applicant.CharId); review.Approve = true;
        Assert.Equal("ApplicationExpired", service.Execute(leader, review).Code);
        Run(service, applicant, Command(ClanCommandType.Apply, id));
        Run(service, leader, Command(ClanCommandType.ReviewApplication, id, applicant.CharId));
        Run(service, applicant, Command(ClanCommandType.Apply, id));
        using (var db = database.CreateDbContext())
            db.ClanApplications.Where(x => x.ClanId == id).ExecuteUpdate(s => s.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        Assert.Equal("ApplicationExpired", service.Execute(leader, review).Code);
        Assert.Empty(Query(service, leader, ClanQueryType.Applications, id).Applications);
    }

    [MySqlFact]
    public void Role_hierarchy_and_funded_dissolution_are_enforced()
    {
        var leader = database.NewActor(); var deputy = database.NewActor(); var service = database.Service();
        var id = Create(service, leader, true); Run(service, deputy, Command(ClanCommandType.Apply, id));
        var role = Command(ClanCommandType.ChangeRole, id, deputy.CharId); role.Role = (int)ClanRole.Deputy; Run(service, leader, role);
        Assert.Equal("RoleHierarchy", service.Execute(deputy, Command(ClanCommandType.Kick, id, leader.CharId)).Code);
        role.RequestId = Guid.NewGuid(); role.Role = (int)ClanRole.Leader;
        Assert.Equal("InvalidRole", service.Execute(leader, role).Code);
        Run(service, deputy, Command(ClanCommandType.Leave, id));
        var donate = Command(ClanCommandType.Donate, id); donate.Amount = 1; Run(service, leader, donate);
        Assert.Equal("ClanHasFunds", service.Execute(leader, Command(ClanCommandType.Dissolve, id)).Code);
    }

    [MySqlFact]
    public async Task Outbox_routes_to_current_and_removed_members_and_marks_delivery()
    {
        var leader = database.NewActor(); var member = database.NewActor(); var service = database.Service();
        var id = Create(service, leader, true); Run(service, member, Command(ClanCommandType.Apply, id));
        Run(service, leader, Command(ClanCommandType.Kick, id, member.CharId));
        var realtime = new ClanRealtimeService(); var received = new System.Collections.Concurrent.ConcurrentDictionary<int, long>();
        realtime.Subscribe(leader.CharId, e => received[leader.CharId] = e.Version);
        realtime.Subscribe(member.CharId, e => received[member.CharId] = e.Version);
        using var worker = new ClanOutboxWorker(database, realtime, Microsoft.Extensions.Logging.Abstractions.NullLogger<ClanOutboxWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (received.Count < 2) await Task.Delay(50, timeout.Token);
            Assert.Equal(3, received[member.CharId]);
            using var db = database.CreateDbContext();
            while (await db.ClanOutboxEvents.AnyAsync(x => x.ClanId == id && x.PublishedAt == null, timeout.Token))
                await Task.Delay(50, timeout.Token);
        }
        finally { await worker.StopAsync(CancellationToken.None); }
    }

    private sealed class FaultFactory(ClanDatabase database, IInterceptor interceptor) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            using var original = database.CreateDbContext();
            return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(original.Database.GetConnectionString(), new MySqlServerVersion(new Version(8, 0, 36)))
                .AddInterceptors(interceptor).Options);
        }
    }
    private sealed class LostCommitAck : DbTransactionInterceptor
    {
        private int remaining = 1;
        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (Interlocked.Exchange(ref remaining, 0) == 1) throw new IOException("Injected loss of COMMIT acknowledgement");
        }
    }
    private sealed class FailReceiptSave : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context!.ChangeTracker.Entries<ClanCommandReceipt>().Any())
                throw new InvalidOperationException("Injected failure after debit, before receipt save");
            return result;
        }
    }

    [MySqlFact]
    public void Realtime_replaced_session_cannot_remove_new_subscription()
    {
        var realtime = new ClanRealtimeService(); var calls = 0;
        Action<ClanChangedDto> old = _ => throw new Exception("Old session");
        Action<ClanChangedDto> current = _ => calls++;
        realtime.Subscribe(1, old); realtime.Subscribe(1, current); realtime.Unsubscribe(1, old);
        realtime.Publish(new[] { 1, 1, 2 }, new ClanChangedDto()); Assert.Equal(1, calls);
    }
}
