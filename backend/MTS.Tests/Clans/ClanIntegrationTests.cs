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
        character.SetMoneyVip(money);
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
    public ClanService Service(int capacity = 30, int creationCost = 10) =>
        new(this, Options.Create(new ClanOptions { InitialCapacity = capacity, DonationUnitMoneyVip = 100, CreationMoneyVipCost = creationCost }), new TestWorld());
    public void Dispose() { using var db = CreateDbContext(); db.Database.EnsureDeleted(); }
}

public sealed class Actor(int id, int money) : IClanActor
{
    public object SyncRoot { get; } = new();
    public int CharId => id;
    public int Level => 0;
    public int MoneyVip { get; set; } = money;
}

public sealed class TestWorld : IClanWorldCatalog { public bool Contains(int nodeId) => nodeId == 1; }

public class ClanIntegrationTests(ClanDatabase database) : IClassFixture<ClanDatabase>
{
    private static ClanCommandRequest Command(ClanCommandType type, int clanId = 0, int target = 0) =>
        new() { RequestId = Guid.NewGuid(), Command = type, ClanId = clanId, TargetCharId = target, CastleNodeId = 1 };
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
    public void Member_query_includes_visuals_and_keeps_members_without_appearance()
    {
        var actor = database.NewActor(); var recruit = database.NewActor(); var service = database.Service();
        var id = Create(service, actor, true); Run(service, recruit, Command(ClanCommandType.Apply, id));
        using (var db = database.CreateDbContext())
        {
            db.Characters.Where(x => x.Id == actor.CharId).ExecuteUpdate(s => s.SetProperty(x => x.Gender, Gender.Female));
            db.Appearances.Add(new Appearance { CharId = actor.CharId, Skins = new() { ["hair"] = "hair-2" } });
            db.SaveChanges();
        }
        var response = Query(service, actor, ClanQueryType.Members, id);
        Assert.True(response.Success, response.Code);
        Assert.Equal(2, response.Members.Length);
        var visual = Assert.Single(response.Members, x => x.CharId == actor.CharId);
        Assert.Equal(Gender.Female, visual.Gender); Assert.Equal("hair-2", visual.Appearance.Skins["hair"]);
        Assert.Empty(Assert.Single(response.Members, x => x.CharId == recruit.CharId).Appearance.Skins);
    }

    [MySqlFact]
    public void Duplicate_donation_debits_once_and_does_not_restore_old_wallet_on_replay()
    {
        var actor = database.NewActor(); var service = database.Service(); var id = Create(service, actor);
        var request = Command(ClanCommandType.Donate, id);
        Run(service, actor, request); Assert.Equal(890, actor.MoneyVip);
        actor.MoneyVip = 850; // a later gameplay purchase
        var replay = Run(service, actor, request); Assert.Equal(850, replay.MoneyVip);
        using var db = database.CreateDbContext();
        Assert.Equal(100, db.Clans.Single(x => x.Id == id).JadeFunds);
        Assert.Equal(890, db.Characters.Single(x => x.Id == actor.CharId).MoneyVip);
        Assert.Equal(4, db.ClanLedgerEntries.Count(x => x.ClanId == id));
        request.DonationCount = 10;
        Assert.Equal("RequestIdReused", service.Execute(actor, request).Code);
    }

    [MySqlFact]
    public void Failed_donation_rolls_back_everything()
    {
        var actor = database.NewActor(50); var service = database.Service(); var id = Create(service, actor);
        var request = Command(ClanCommandType.Donate, id);
        Assert.Equal("InsufficientFunds", service.Execute(actor, request).Code);
        using var db = database.CreateDbContext();
        Assert.Equal(0, db.Clans.Single(x => x.Id == id).Funds);
        Assert.Equal(40, actor.MoneyVip);
        Assert.Empty(db.ClanLedgerEntries.Where(x => x.RequestId == request.RequestId));
        Assert.Empty(db.ClanMemberPeriodStats.Where(x => x.CharId == actor.CharId));
        Assert.DoesNotContain(db.ClanCommandReceipts, x => x.RequestId == request.RequestId);
    }

    [MySqlFact]
    public void Free_donation_is_once_daily_and_paid_batches_have_no_daily_cap()
    {
        var actor = database.NewActor(5000); var service = database.Service(); var id = Create(service, actor);
        var request = Command(ClanCommandType.Donate, id); request.FreeDonation = true; Run(service, actor, request);
        Assert.Equal(4990, actor.MoneyVip);
        request.RequestId = Guid.NewGuid();
        Assert.Equal("FreeDonationUsed", service.Execute(actor, request).Code);
        for (var i = 0; i < 2; i++)
        {
            request = Command(ClanCommandType.Donate, id); request.DonationCount = 10; Run(service, actor, request);
        }
        Assert.Equal(2990, actor.MoneyVip);
        using var db = database.CreateDbContext();
        Assert.All(db.ClanMemberPeriodStats.Where(x => x.CharId == actor.CharId), x => Assert.Equal(0, x.ScopeId));
        Assert.Equal(2100, db.Clans.Single(x => x.Id == id).JadeFunds);
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
        var donate = Command(ClanCommandType.Donate, id); Run(service, leader, donate);
        var publicDetail = Query(service, outsider, ClanQueryType.Detail, id).Detail;
        Assert.Equal("", publicDetail.Announcement); Assert.Equal(0, publicDetail.Funds); Assert.Equal(-1, publicDetail.MyRole);
        Assert.Equal(0, publicDetail.JadeFunds); Assert.Null(publicDetail.CastleNodeId);
        Assert.Equal(1, Query(service, leader, ClanQueryType.Mine).Detail.CastleNodeId);
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
        Assert.Equal(40, actor.MoneyVip);
        var poor = database.NewActor(10); var request = Command(ClanCommandType.Create); request.Name = "Poor" + Guid.NewGuid().ToString("N");
        Assert.Equal("InsufficientFunds", service.Execute(poor, request).Code);
        using var db = database.CreateDbContext();
        Assert.False(db.Clans.Any(x => x.Name == request.Name));
        Assert.False(db.ClanMembers.Any(x => x.CharId == poor.CharId));
        Assert.Equal(40, db.Characters.Single(x => x.Id == actor.CharId).MoneyVip);
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
        CharacterData stats;
        using (var db = database.CreateDbContext()) stats = db.Characters.AsNoTracking().Single(x => x.Id == actor.CharId);
        stats.SetMoneyVip(800); // gameplay change not yet flushed by periodic autosave
        var live = new StatsActor(stats);
        var request = Command(ClanCommandType.Donate, id);
        Assert.True(service.Execute(live, request).Success);
        Assert.Equal(700, stats.MoneyVip);
        lock (live.SyncRoot)
        {
            using var autosave = database.CreateDbContext();
            stats.SaveChanges(autosave).GetAwaiter().GetResult();
        }
        using var verify = database.CreateDbContext();
        Assert.Equal(700, verify.Characters.Single(x => x.Id == actor.CharId).MoneyVip);
        Assert.Equal(100, verify.Clans.Single(x => x.Id == id).JadeFunds);
    }

    private sealed class StatsActor(CharacterData stats) : IClanActor
    {
        public object SyncRoot { get; } = new();
        public int CharId => stats.Id;
        public int Level => 0;
        public int MoneyVip { get => stats.MoneyVip; set => stats.SetMoneyVip(value); }
    }

    [MySqlFact]
    public void Lost_commit_ack_recovers_receipt_and_updates_live_balance()
    {
        var actor = database.NewActor(); var normal = database.Service(); var id = Create(normal, actor);
        var interceptor = new LostCommitAck();
        var service = new ClanService(new FaultFactory(database, interceptor), Options.Create(new ClanOptions { DonationUnitMoneyVip = 100 }), new TestWorld());
        var request = Command(ClanCommandType.Donate, id);
        Run(service, actor, request);
        Assert.Equal(890, actor.MoneyVip);
        using var db = database.CreateDbContext();
        Assert.Equal(100, db.Clans.Single(x => x.Id == id).JadeFunds);
        Assert.Equal(890, db.Characters.Single(x => x.Id == actor.CharId).MoneyVip);
        Assert.Single(db.ClanCommandReceipts.Where(x => x.RequestId == request.RequestId));
    }

    [MySqlFact]
    public void Failure_after_wallet_update_rolls_back_money_and_clan()
    {
        var actor = database.NewActor(); var normal = database.Service(); var id = Create(normal, actor);
        var service = new ClanService(new FaultFactory(database, new FailReceiptSave()), Options.Create(new ClanOptions { DonationUnitMoneyVip = 100 }), new TestWorld());
        var request = Command(ClanCommandType.Donate, id);
        Assert.Throws<InvalidOperationException>(() => service.Execute(actor, request));
        using var db = database.CreateDbContext();
        Assert.Equal(990, actor.MoneyVip);
        Assert.Equal(990, db.Characters.Single(x => x.Id == actor.CharId).MoneyVip);
        Assert.Equal(0, db.Clans.Single(x => x.Id == id).Funds);
        Assert.Empty(db.ClanLedgerEntries.Where(x => x.RequestId == request.RequestId));
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
        var donate = Command(ClanCommandType.Donate, id); Run(service, leader, donate);
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

    [MySqlFact]
    public void Creation_requires_configured_price_valid_node_and_payment_method()
    {
        var actor = database.NewActor(); var request = Command(ClanCommandType.Create);
        request.Name = "Rules" + Guid.NewGuid().ToString("N");
        Assert.Equal("CreationPriceNotConfigured", database.Service(creationCost: 0).Execute(actor, request).Code);
        request.CastleNodeId = -1;
        Assert.Equal("InvalidCastleNode", database.Service().Execute(actor, request).Code);
        request.CastleNodeId = 1; request.CreationPaymentMethod = 2;
        Assert.Equal("InvalidPaymentMethod", database.Service().Execute(actor, request).Code);
        Assert.Equal(1000, actor.MoneyVip);
    }

    [MySqlFact]
    public void Donation_rejects_client_amount_and_invalid_batch()
    {
        var actor = database.NewActor(); var service = database.Service(); var id = Create(service, actor);
        var request = Command(ClanCommandType.Donate, id); request.Amount = 1;
        Assert.False(service.Execute(actor, request).Success);
        request.Amount = 0; request.DonationCount = 2;
        Assert.Equal("InvalidDonationCount", service.Execute(actor, request).Code);
        request.FreeDonation = true; request.DonationCount = 10;
        Assert.Equal("InvalidDonationCount", service.Execute(actor, request).Code);
        Assert.True(Query(service, actor, ClanQueryType.Mine).Detail.FreeDonationAvailable);
        request.DonationCount = 1; Run(service, actor, request);
        Assert.False(Query(service, actor, ClanQueryType.Mine).Detail.FreeDonationAvailable);
    }

    [MySqlFact]
    public void Inheritance_unlock_learn_select_and_leave_enforce_costs_and_access()
    {
        var settings = Options.Create(new ClanOptions { CreationMoneyVipCost = 10, DonationUnitMoneyVip = 100,
            Inheritances = [new ClanInheritanceDefinition { Id = 1, Name = "Kim", Effect = 1, Element = 1,
                Value = 2, UnlockCost = 100, LearnCost = 50 }] });
        var service = new ClanService(database, settings, new TestWorld());
        var leader = database.NewActor(); var member = database.NewActor(); var outsider = database.NewActor();
        var id = Create(service, leader, true); Run(service, member, Command(ClanCommandType.Apply, id));
        var unlock = Command(ClanCommandType.UnlockInheritance, id); unlock.InheritanceId = 1;
        Assert.Equal("Forbidden", service.Execute(member, unlock).Code);
        Assert.Equal("InsufficientClanFunds", service.Execute(leader, unlock).Code);
        var learn = Command(ClanCommandType.LearnInheritance, id); learn.InheritanceId = 1;
        Assert.Equal("InheritanceLocked", service.Execute(member, learn).Code);
        Run(service, member, Command(ClanCommandType.Donate, id)); Run(service, leader, unlock);
        Run(service, leader, unlock); // receipt replay must not debit twice
        Run(service, member, learn); Run(service, member, learn);
        var mine = Query(service, member, ClanQueryType.Mine).Detail;
        Assert.Equal(0, mine.JadeFunds); Assert.Equal(50, mine.MyAvailableContribution); Assert.Equal(1, mine.ActiveInheritanceId);
        var inheritance = Assert.Single(Query(service, member, ClanQueryType.Inheritances, id).Inheritances);
        Assert.True(inheritance.Unlocked); Assert.True(inheritance.Learned);
        Assert.Equal("NotMember", Query(service, outsider, ClanQueryType.Inheritances, id).Code);
        Run(service, member, Command(ClanCommandType.SelectInheritance, id));
        Assert.Null(Query(service, member, ClanQueryType.Mine).Detail.ActiveInheritanceId);
        Run(service, member, Command(ClanCommandType.Leave, id));
        using var db = database.CreateDbContext();
        Assert.False(db.ClanMemberInheritances.Any(x => x.CharId == member.CharId));
    }

    private sealed class FakeGateway : IClanPaymentGateway
    {
        public int Calls;
        public Task<ClanCheckout> CreateOrRecover(long orderId, long amount, DateTime expiresAt)
        {
            Calls++;
            return Task.FromResult(new ClanCheckout("link-" + orderId, "https://example.com/checkout/" + orderId));
        }
    }

    [MySqlFact]
    public async Task Paid_creation_is_owned_consumed_once_and_survives_rejected_creation()
    {
        var actor = database.NewActor(); var outsider = database.NewActor(); var gateway = new FakeGateway();
        var payments = new ClanPaymentService(database, Options.Create(new ClanOptions { CreationPayOsVnd = 20000 }),
            new MTS.Services.SnowflakeIdGenerator(1), gateway, new ClanRealtimeService());
        var requestId = Guid.NewGuid(); var response = await payments.Prepare(actor.CharId, requestId);
        var order = Assert.Single(response.Payments);
        Assert.Equal(order.OrderId, Assert.Single((await payments.Prepare(actor.CharId, requestId)).Payments).OrderId);
        Assert.Equal(1, gateway.Calls);
        var service = database.Service();
        Assert.Single(Query(service, actor, ClanQueryType.Payments).Payments);
        Assert.Empty(Query(service, outsider, ClanQueryType.Payments).Payments);
        var create = Command(ClanCommandType.Create); create.Name = "Paid" + Guid.NewGuid().ToString("N");
        create.CreationPaymentMethod = 1; create.PaymentOrderId = order.OrderId;
        Assert.Equal("PaidCreationRequired", service.Execute(actor, create).Code);
        Assert.Throws<ClanRuleException>(() => payments.AcceptVerified(order.OrderId, 1, "VND", "00", "link-" + order.OrderId, "ref"));
        Assert.True(payments.AcceptVerified(order.OrderId, order.Amount, "VND", "00", "link-" + order.OrderId, "ref"));
        Assert.True(payments.AcceptVerified(order.OrderId, order.Amount, "VND", "00", "link-" + order.OrderId, "ref"));
        Assert.Equal("PaidCreationRequired", service.Execute(outsider, create).Code);
        create.Name = "x"; Assert.Equal("InvalidName", service.Execute(actor, create).Code);
        create.Name = "Paid" + Guid.NewGuid().ToString("N");
        var created = Run(service, actor, create); Run(service, actor, create);
        Assert.Equal(1000, actor.MoneyVip);
        Assert.True(payments.AcceptVerified(order.OrderId, order.Amount, "VND", "00", "link-" + order.OrderId, "ref"));
        var paid = Assert.Single(Query(service, actor, ClanQueryType.Payments).Payments);
        Assert.Equal((int)ClanCreationPaymentStatus.Consumed, paid.Status); Assert.Equal(created.ClanId, paid.ClanId);
    }

    [MySqlFact]
    public async Task Expired_checkout_is_rejected_but_delayed_verified_payment_is_honored()
    {
        var actor = database.NewActor();
        var payments = new ClanPaymentService(database, Options.Create(new ClanOptions { CreationPayOsVnd = 10000 }),
            new MTS.Services.SnowflakeIdGenerator(2), new FakeGateway(), new ClanRealtimeService());
        var requestId = Guid.NewGuid(); var order = Assert.Single((await payments.Prepare(actor.CharId, requestId)).Payments);
        using (var db = database.CreateDbContext())
            db.ClanCreationPayments.Where(x => x.OrderId == order.OrderId).ExecuteUpdate(s => s.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        var error = await Assert.ThrowsAsync<ClanRuleException>(() => payments.Prepare(actor.CharId, requestId));
        Assert.Equal("PaymentLinkExpired", error.Code);
        Assert.True(payments.AcceptVerified(order.OrderId, order.Amount, "VND", "00", "link-" + order.OrderId, "late"));
        Assert.Equal((int)ClanCreationPaymentStatus.Paid, Assert.Single((await payments.Prepare(actor.CharId, requestId)).Payments).Status);
    }
}
