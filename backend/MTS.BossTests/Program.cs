using MemoryPack;
using Microsoft.Extensions.Configuration;
using MTS.Exceptions;
using MTS.Game.Boss;
using MTS.Game.AllMaps;
using MTS.Game.Combat;
using MTS.Game.LuyenDan;
using MTS.Game.Rank;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using MTS.Services;

// Snapshot construction reads the clan whitelist; these tests do not load game data or a database.
var clanResources = new MTS.Game.Clan.ClanBuffResources();
typeof(MTS.Game.Clan.ClanBuffResources).GetProperty("EffectTypeConfigs")!
    .SetValue(clanResources, Array.Empty<MTS.Game.LuyenKhi.EffectTypeConfig>());
typeof(MTS.Game.GameConfig).GetProperty("ClanBuffResources")!.SetValue(null, clanResources);
var furnaceResources = new LuyenDanResources();
var furnaceTemplates = new[] { Tier.TamPham, Tier.TuPham, Tier.NguPham, Tier.LucPham }
    .Select((tier, index) =>
    {
        var template = new TempFurnaceData();
        typeof(TempFurnaceData).GetProperty("Id")!.SetValue(template, 1003 + index);
        typeof(TempFurnaceData).GetProperty("Tier")!.SetValue(template, tier);
        return template;
    }).ToList();
typeof(LuyenDanResources).GetProperty("TempFurnaceDatas")!.SetValue(furnaceResources, furnaceTemplates);
typeof(MTS.Game.GameConfig).GetProperty("LuyenDanResources")!.SetValue(null, furnaceResources);

var tests = new (string Name, Action Run)[]
{
    ("schedule boundaries and Vietnam timezone", Schedule),
    ("schedule text reflects configured days, hours and open-ended sessions", ScheduleText),
    ("ready duplicates, reconnect and three attempts", Attempts),
    ("attempts reset at midnight Vietnam time", DailyAttemptReset),
    ("reject invalid, forged and premature damage", DamageValidation),
    ("simultaneous kills persist once and include departed participants", ConcurrentKill),
    ("result validates stats and grants no reward", Result),
    ("failed persistence leaves HP and attempts unchanged", FailedSave),
    ("restore killed boss and exhausted attempts", Restore),
    ("closing a boss prevents further damage and participation", Close),
    ("boss snapshot reflects shared HP", Snapshot),
    ("boss ends while player loads without charging another attempt", LateReady),
    ("reward mail includes both portions only for the killer", RewardMail),
    ("all historical participants receive mail and killer gets both rewards", HistoricalParticipantMail),
    ("active boss created before reward setup still pays both portions", LegacyRewardMail),
    ("damage raises each rare furnace chance while total stays at 100%", FurnaceChances),
    ("first boss fight grants 100 jade once per Vietnam calendar day", DailyFirstReward),
    ("spawn excludes cities and occupied clan nodes", SpawnNodes),
    ("join checks current node, spawn identity and player HP", JoinValidation),
    ("popup counts only connected fighters and does not consume attempts", PopupStatus),
    ("damage totals credit only real HP loss", DamageTotals)
};
foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}
Console.WriteLine($"{tests.Length} boss checks passed.");

static void Check(bool condition)
{
    if (!condition) throw new Exception("Assertion failed");
}
static void Reject(Action action)
{
    try { action(); }
    catch (InvalidException) { return; }
    throw new Exception("Expected rejection");
}
static CharacterData Character(int id)
{
    var data = new CharacterData { Id = id, Name = $"Character {id}" };
    typeof(CharacterStats).GetProperty(nameof(CharacterStats.SinhMenh))!.SetValue(data.Stats, 1000);
    data.Stats.SetHp(1000);
    data.Stats.SetThanThuc(100);
    data.Stats.SetThoNguyen(100);
    data.Progression.MaxLevel = 4;
    return data;
}
static BossData Data() => new(ETypeVisualChar.EnemyLuyenKhiSoKi, new CharacterSnapshot(Character(-100)));
static BossManager Boss(Action<int, Dictionary<int, int>, Dictionary<int, long>, int?>? save = null) => new(Data(), save);
static Fighter Join(BossManager boss, int id)
{
    var player = new Fighter(Character(id));
    boss.OnPlayerJoin(player);
    Check(boss.OnPlayerReady(player));
    return player;
}
static SyncDame Hit(int from, int damage) => new()
{
    CharSendDame = from, CharTakeDame = -100,
    Datas = [new DameData { Dame = damage }]
};
static ResultCombat Lose(Fighter player) => new()
{
    TypeCombat = ETypeCombat.Boss, TypeResultCombat = ETypeResultCombat.LoseByTime,
    CurrentHPUser = 200, CurrentThanThuc = 90, CurrentThoNguyen = 90,
    CurrentLevelEnemy = 0, ListIdThanThong = []
};
static void Schedule()
{
    var days = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday };
    var start = new TimeOnly(12, 0);
    var end = new TimeOnly(13, 0);
    var mondayStart = new DateTimeOffset(2026, 9, 21, 5, 0, 0, TimeSpan.Zero);
    var window = BossService.GetActiveWindow(mondayStart, days, start, end);
    Check(window?.Start == mondayStart && window?.End == mondayStart.AddHours(1));
    Check(BossService.GetActiveWindow(mondayStart.AddSeconds(-1), days, start, end) == null);
    Check(BossService.GetActiveWindow(mondayStart.AddHours(1), days, start, end) == null);
    Check(BossService.GetActiveWindow(mondayStart.AddDays(1), days, start, end) == null);
    window = BossService.GetActiveWindow(mondayStart.AddDays(2).AddMinutes(30), days, start, end);
    Check(window?.Start == mondayStart.AddDays(2));
    Check(BossService.GetActiveWindow(mondayStart, [], start, end) == null);
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Boss:DaysOfWeek:0"] = "Monday",
        ["Boss:DaysOfWeek:1"] = "Friday",
        ["Boss:StartTime"] = "19:30:00",
        ["Boss:EndTime"] = "20:45:00",
        ["Boss:KeepUntilNextSpawn"] = "true"
    }).Build();
    var options = config.GetSection("Boss").Get<BossOptions>()!;
    Check(options.DaysOfWeek.SequenceEqual([DayOfWeek.Monday, DayOfWeek.Friday]) &&
          options.StartTime == new TimeOnly(19, 30) && options.EndTime == new TimeOnly(20, 45) &&
          options.KeepUntilNextSpawn);

    // Without an end time, the Monday spawn remains until Wednesday's spawn.
    var persistent = BossService.GetActiveWindow(mondayStart.AddDays(1), days, start, end, true);
    Check(persistent?.Start == mondayStart && persistent?.End == DateTimeOffset.MaxValue);
    persistent = BossService.GetActiveWindow(mondayStart.AddDays(2).AddSeconds(-1), days, start, end, true);
    Check(persistent?.Start == mondayStart);
    persistent = BossService.GetActiveWindow(mondayStart.AddDays(2), days, start, end, true);
    Check(persistent?.Start == mondayStart.AddDays(2));
    persistent = BossService.GetActiveWindow(mondayStart.AddSeconds(-1),
        [DayOfWeek.Monday], start, start, true);
    Check(persistent?.Start == mondayStart.AddDays(-7));
    Check(BossService.GetActiveWindow(mondayStart, [], start, start, true) == null);

    // A late Monday spawn can remain active into Tuesday morning.
    var overnightStart = new DateTimeOffset(2026, 9, 21, 15, 0, 0, TimeSpan.Zero);
    window = BossService.GetActiveWindow(overnightStart.AddHours(6),
        [DayOfWeek.Monday], new TimeOnly(22, 0), new TimeOnly(6, 0));
    Check(window?.Start == overnightStart && window?.End == overnightStart.AddHours(8));
    Check(BossService.GetActiveWindow(overnightStart.AddHours(8),
        [DayOfWeek.Monday], new TimeOnly(22, 0), new TimeOnly(6, 0)) == null);
    try
    {
        BossService.GetActiveWindow(mondayStart, days, start, start);
        throw new Exception("Expected invalid equal times");
    }
    catch (InvalidOperationException) { }
}
static void ScheduleText()
{
    var options = new BossOptions
    {
        DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday],
        StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0)
    };
    Check(options.GetScheduleText() == "Lịch Boss (giờ VN): Thứ 2, Thứ 4, 12:00–13:00");
    options.KeepUntilNextSpawn = true;
    Check(options.GetScheduleText() == "Lịch Boss (giờ VN): Thứ 2, Thứ 4, từ 12:00 đến phiên kế tiếp");
    options.DaysOfWeek = Enum.GetValues<DayOfWeek>();
    Check(options.GetScheduleText().Contains("Hằng ngày"));
    options.DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday];
    Check(options.GetScheduleText().Contains("T2, T3, T4, T5, T6, T7"));
}
static void Attempts()
{
    var boss = Boss();
    for (var i = 0; i < 3; i++)
    {
        var player = Join(boss, 1);
        Check(!boss.OnPlayerReady(player));
        Reject(() => boss.OnPlayerJoin(new Fighter(Character(1))));
        boss.OnPlayerLeave(player);
    }
    Reject(() => boss.OnPlayerJoin(new Fighter(Character(1))));
    Join(Boss(), 1); // New spawn has its own quota.
}
static void DailyAttemptReset()
{
    var now = new DateTimeOffset(2026, 9, 22, 16, 59, 0, TimeSpan.Zero); // 23:59 giờ Việt Nam.
    var boss = new BossManager(Data(), utcNow: () => now);
    for (var i = 0; i < BossManager.MaxAttempts; i++)
    {
        var player = Join(boss, 1);
        boss.OnPlayerLeave(player);
    }
    Check(boss.GetStatus(1).RemainingAttempts == 0);
    Reject(() => boss.OnPlayerJoin(new Fighter(Character(1))));

    now = now.AddMinutes(2); // 00:01 ngày kế tiếp tại Việt Nam.
    Check(boss.GetStatus(1).RemainingAttempts == BossManager.MaxAttempts);
    var nextDay = Join(boss, 1);
    Check(boss.GetStatus(1).RemainingAttempts == BossManager.MaxAttempts - 1);
    boss.OnPlayerLeave(nextDay);

    var progress = new BossProgress
    {
        BossBytes = MemoryPackSerializer.Serialize(Data()), Hp = 1000,
        AttemptsJson = "{\"1\":3}", AttemptDaysJson = "{\"1\":20260922}"
    };
    var restored = new BossManager(progress, DateTimeOffset.MaxValue,
        () => new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
    Check(restored.GetStatus(1).RemainingAttempts == 0);
    restored = new BossManager(progress, DateTimeOffset.MaxValue,
        () => new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));
    Check(restored.GetStatus(1).RemainingAttempts == BossManager.MaxAttempts);
    progress.AttemptDaysJson = "{}"; // Phiên cũ trước migration được reset một lần an toàn.
    restored = new BossManager(progress, DateTimeOffset.MaxValue,
        () => new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
    Check(restored.GetStatus(1).RemainingAttempts == BossManager.MaxAttempts);
}
static void DamageValidation()
{
    var boss = Boss();
    var player = new Fighter(Character(1));
    boss.OnPlayerJoin(player);
    boss.TakeDame(player, Hit(1, 10));
    Check(boss.Character.Hp == 1000);
    Check(boss.OnPlayerReady(player));
    foreach (var hit in new[] { Hit(2, 10), Hit(1, -10), Hit(1, 1_000_001), Hit(1, int.MaxValue) })
        boss.TakeDame(player, hit);
    var wrongTarget = Hit(1, 10); wrongTarget.CharTakeDame = 1;
    boss.TakeDame(player, wrongTarget);
    var invalid = Hit(1, 10); invalid.Datas = null!;
    boss.TakeDame(player, invalid);
    invalid.Datas = Enumerable.Repeat(new DameData { Dame = 1 }, 101).ToList();
    boss.TakeDame(player, invalid);
    boss.TakeDame(new Fighter(Character(1)), Hit(1, 100));
    Check(boss.Character.Hp == 1000);
    boss.TakeDame(player, Hit(1, 100));
    Check(boss.Character.Hp == 900 && boss.GetDamageTotals()[1] == 100);
    boss.OnPlayerLeave(player);
    boss.TakeDame(player, Hit(1, 100));
    Check(boss.Character.Hp == 900 && boss.GetDamageTotals()[1] == 100);
}
static void ConcurrentKill()
{
    var kills = 0;
    int[] participants = [];
    var boss = Boss((hp, attempts, damage, killer) =>
    {
        if (killer.HasValue) { Interlocked.Increment(ref kills); participants = attempts.Keys.ToArray(); }
    });
    var departed = Join(boss, 1);
    boss.OnPlayerLeave(departed);
    var players = Enumerable.Range(2, 10).Select(id => Join(boss, id)).ToArray();
    Parallel.ForEach(players, p => boss.TakeDame(p, Hit(p.Id, 1000)));
    Check(kills == 1 && boss.Character.Hp == 0 && participants.Length == 11 && participants.Contains(1));
    Reject(() => boss.OnPlayerJoin(new Fighter(Character(12))));
}
static void DamageTotals()
{
    var boss = Boss();
    var first = Join(boss, 1);
    var second = Join(boss, 2);
    boss.TakeDame(first, Hit(1, 350));
    boss.TakeDame(second, Hit(2, 1000));
    Check(boss.Character.Hp == 0);
    Check(boss.GetDamageTotals()[1] == 350 && boss.GetDamageTotals()[2] == 650);
    boss.OnPlayerLeave(first);
    Check(boss.GetDamageTotals()[1] == 350);
}

static void Result()
{
    var saves = 0;
    var boss = Boss((_, _, _, _) => saves++);
    var player = Join(boss, 1);
    var result = Lose(player);
    result.CurrentHPUser = -1;
    Reject(() => boss.OnResultCombat(player, result));
    Check(player.CharData.Stats.Hp == 1000);
    result = Lose(player); result.TypeResultCombat = ETypeResultCombat.Win;
    Reject(() => boss.OnResultCombat(player, result));
    result = Lose(player); result.CurrentThanThuc = 101;
    Reject(() => boss.OnResultCombat(player, result));
    var response = boss.OnResultCombat(player, Lose(player));
    Check(response.Success && response.NewHP == 200 && response.Reward.Money == 0 && response.Reward.MoneyVip == 0);
    Check(saves == 1 && boss.GetPlayerCount() == 0);
    Reject(() => boss.OnResultCombat(player, Lose(player)));
    boss.TakeDame(player, Hit(1, 1000));
    Check(boss.Character.Hp == 1000);
}
static void FailedSave()
{
    var fail = true;
    var boss = Boss((_, _, _, _) => { if (fail) throw new IOException("Simulated DB failure"); });
    var player = new Fighter(Character(1));
    boss.OnPlayerJoin(player);
    try { boss.OnPlayerReady(player); throw new Exception("Expected I/O failure"); } catch (IOException) { }
    fail = false;
    Check(boss.OnPlayerReady(player));
    fail = true;
    try { boss.TakeDame(player, Hit(1, 1000)); throw new Exception("Expected I/O failure"); } catch (IOException) { }
    Check(boss.Character.Hp == 1000 && boss.GetDamageTotals().Count == 0);
    fail = false;
    boss.TakeDame(player, Hit(1, 1000));
    Check(boss.Character.Hp == 0 && boss.GetDamageTotals()[1] == 1000);
}
static void Restore()
{
    var progress = new BossProgress
    {
        BossBytes = MemoryPackSerializer.Serialize(Data()), Hp = 500,
        AttemptsJson = "{\"1\":3}", AttemptDaysJson = "{\"1\":20260923}", DamageJson = "{\"1\":450}"
    };
    var boss = new BossManager(progress, DateTimeOffset.MaxValue,
        () => new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));
    Check(boss.Character.Hp == 500 && boss.GetDamageTotals()[1] == 450);
    Reject(() => boss.OnPlayerJoin(new Fighter(Character(1))));
    progress.Hp = 0; progress.KillerId = 2;
    boss = new BossManager(progress, DateTimeOffset.MaxValue,
        () => new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));
    Reject(() => boss.OnPlayerJoin(new Fighter(Character(2))));
}
static void Close()
{
    var boss = Boss();
    var player = Join(boss, 1);
    boss.Close();
    boss.TakeDame(player, Hit(1, 1000));
    Check(boss.Character.Hp == 1000);
    Reject(() => boss.OnPlayerJoin(new Fighter(Character(2))));
    Check(boss.OnResultCombat(player, Lose(player)).Success);
}
static void Snapshot()
{
    var boss = Boss(); var player = Join(boss, 1);
    boss.TakeDame(player, Hit(1, 123));
    Check(boss.GetBossData().Character.Hp == 877);
}
static void LateReady()
{
    var saves = 0;
    var boss = Boss((_, _, _, _) => saves++);
    var player = new Fighter(Character(1));
    boss.OnPlayerJoin(player);
    boss.Close();
    Check(boss.OnPlayerReady(player));
    Check(boss.OnResultCombat(player, Lose(player)).Success);
    Check(saves == 0);
}
static void RewardMail()
{
    var progress = new BossProgress
    {
        BossBytes = MemoryPackSerializer.Serialize(Data()),
        ParticipantRewards = MemoryPackSerializer.Serialize<List<IMailReward>>([new MailRewardMoney { Money = 100 }]),
        KillerRewards = MemoryPackSerializer.Serialize<List<IMailReward>>([new MailRewardMoneyVip { MoneyVip = 50 }])
    };
    var mails = BossStore.CreateRewardMails(progress, [1, 2, 2], new Dictionary<int, long> { [1] = 300, [2] = 700 }, 2);
    Check(mails.Count == 2);
    Check(mails.Single(x => x.CharId == 1).Rewards.Count == 2);
    var rewards = mails.Single(x => x.CharId == 2).Rewards;
    Check(rewards.Count == 3 && rewards.OfType<MailRewardMoney>().Single().Money == 100 &&
          rewards.OfType<MailRewardMoneyVip>().Single().MoneyVip == 50);
    Check(!ReferenceEquals(mails[0].Rewards[0], mails[1].Rewards[0]));
}
static void HistoricalParticipantMail()
{
    var progress = new BossProgress
    {
        BossBytes = MemoryPackSerializer.Serialize(Data()),
        ParticipantRewards = MemoryPackSerializer.Serialize(BossRewards.CreateParticipantRewards(Data())),
        KillerRewards = MemoryPackSerializer.Serialize(BossRewards.CreateKillerRewards(Data()))
    };
    var mails = new List<Mail>();
    var boss = Boss((_, attempts, damage, killer) =>
    {
        if (killer.HasValue) mails.AddRange(BossStore.CreateRewardMails(progress, attempts.Keys, damage, killer.Value));
    });
    var departed = Join(boss, 1);
    boss.OnPlayerLeave(departed);
    var finished = Join(boss, 2);
    boss.OnResultCombat(finished, Lose(finished));
    var rejoined = Join(boss, 1);
    boss.OnPlayerLeave(rejoined);
    var killer = Join(boss, 3);
    // A character who has not readied has not participated yet.
    boss.OnPlayerJoin(new Fighter(Character(4)));
    boss.TakeDame(killer, Hit(3, 1000));
    boss.TakeDame(killer, Hit(3, 1000));
    Check(mails.Select(x => x.CharId).Order().SequenceEqual(new[] { 1, 2, 3 }));
    Check(mails.All(x => x.Rewards.OfType<MailRewardMoneyVip>().Any(r => r.MoneyVip == 1000)));
    Check(mails.All(x => x.Rewards.OfType<MailRewardFurnace>().Any()));
    var killerMail = mails.Single(x => x.CharId == 3);
    Check(killerMail.Rewards.OfType<MailRewardMoneyVip>().Any(r => r.MoneyVip == 5000));
    Check(killerMail.Rewards.OfType<MailRewardFurnace>().Count() == 2);
    Check(killerMail.Rewards.OfType<MailRewardFurnace>().Any(r => r.FurnaceData.Id == 1006));
}
static void LegacyRewardMail()
{
    var progress = new BossProgress
    {
        BossBytes = MemoryPackSerializer.Serialize(Data()),
        ParticipantRewards = MemoryPackSerializer.Serialize<List<IMailReward>>([]),
        KillerRewards = MemoryPackSerializer.Serialize<List<IMailReward>>([])
    };
    var mail = BossStore.CreateRewardMails(progress, [1], new Dictionary<int, long> { [1] = 1000 }, 1).Single();
    Check(mail.Rewards.OfType<MailRewardMoneyVip>().Select(x => x.MoneyVip).Order()
        .SequenceEqual(new[] { 1000, 5000 }));
    Check(mail.Rewards.OfType<MailRewardFurnace>().Count() == 2);
}
static void FurnaceChances()
{
    var initial = BossRewards.GetFurnaceChances(0, 1000);
    Check(initial == (9600, 200, 150, 50));
    var oneStep = BossRewards.GetFurnaceChances(1, 1000);
    Check(oneStep == (9570, 210, 160, 60));
    var belowStep = BossRewards.GetFurnaceChances(0, 1000);
    Check(belowStep == initial);
    var cap = BossRewards.GetFurnaceChances(1000, 1000);
    Check(cap == (0, 3400, 3350, 3250));
    Check(cap.TamPham + cap.TuPham + cap.NguPham + cap.LucPham == 10000);
}
static void DailyFirstReward()
{
    var boss = Boss();
    var player = Join(boss, 1);
    Check(player.CharData.MoneyVip == 100);
    boss.OnPlayerLeave(player);
    boss.OnPlayerJoin(player);
    Check(boss.OnPlayerReady(player));
    Check(player.CharData.MoneyVip == 100);
    boss.OnPlayerLeave(player);
    player.CharData.SetLastDailyBossRewardAt(DateTime.UtcNow.AddDays(-1));
    boss.OnPlayerJoin(player);
    Check(boss.OnPlayerReady(player));
    Check(player.CharData.MoneyVip == 200);
}
static void SpawnNodes()
{
    var maps = new Map[]
    {
        new NormalMap { Id = 1 }, new NormalMap { Id = 2 },
        new Stronghold { Id = 3, MapType = MapType.Stronghold },
        new Map { Id = 4, MapType = MapType.Stronghold },
        new NormalMap { Id = 5 }, new NormalMap { Id = 0 }
    };
    Check(BossStore.GetSpawnNodeIds(maps, [2, 5]).SequenceEqual(new[] { 1 }));
    Check(BossStore.GetSpawnNodeIds(maps, [1, 2, 5]).Length == 0);
}
static void JoinValidation()
{
    var boss = Boss();
    typeof(BossManager).GetProperty(nameof(BossManager.SpawnId))!.SetValue(boss, 99L);
    typeof(BossManager).GetProperty(nameof(BossManager.NodeId))!.SetValue(boss, 10);
    var player = new Fighter(Character(1));
    player.CharData.Progression.SetIdCurrentNode(11);
    var request = new JoinBossRequest { SpawnId = 99, NodeId = 10 };
    Reject(() => boss.Join(player, request));
    player.CharData.Progression.SetIdCurrentNode(10);
    request.SpawnId = 98;
    Reject(() => boss.Join(player, request));
    request.SpawnId = 99; request.NodeId = 11;
    Reject(() => boss.Join(player, request));
    request.NodeId = 10; player.CharData.Stats.SetHp(0);
    Reject(() => boss.Join(player, request));
    player.CharData.Stats.SetHp(100);
    boss.Join(player, request);
    Check(boss.GetStatus(1).RemainingAttempts == 3);
    Check(boss.OnPlayerReady(player));
    Check(boss.GetStatus(1).RemainingAttempts == 2);
}
static void PopupStatus()
{
    var boss = Boss();
    var loading = new Fighter(Character(1)); boss.OnPlayerJoin(loading);
    Check(boss.GetStatus(1).FightingCount == 0);
    Check(boss.GetStatus(1).RemainingAttempts == 3);
    var fighting = Join(boss, 2);
    Check(boss.GetStatus(1).FightingCount == 1);
    fighting.IsConnected = false;
    Check(boss.GetStatus(1).FightingCount == 0);
    fighting.IsConnected = true;
    boss.OnPlayerLeave(fighting);
    Check(boss.GetStatus(1).FightingCount == 0);
    Check(boss.GetStatus(2).RemainingAttempts == 2);
}
sealed class Fighter(CharacterData data) : IBossFighter
{
    public bool IsConnected { get; set; } = true;
    public int Id => data.Id;
    public CharacterData CharData => data;
    public object MainLock { get; } = new();
    public void Send(Span<byte> data) { }
}
