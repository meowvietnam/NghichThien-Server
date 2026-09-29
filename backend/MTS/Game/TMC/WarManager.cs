using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using MTS.Services;
using Serilog;

namespace MTS.Game.TMC;

/// <summary>
/// Trạng thái Tông môn chiến do server quyết định: owner node, vị trí/hành trình/HP người và rối, trận đấu.
/// Mọi thay đổi chạy tuần tự dưới <see cref="Sync"/> nên thứ tự owner/vị trí/kết quả luôn nhất quán;
/// mỗi thao tác kết thúc bằng một <see cref="WarDelta"/> có version tăng dần gửi cho mọi người đang ở scene.
/// Dữ liệu được ghi bền bởi <see cref="Flush"/> (gọi định kỳ từ TongMonChienService).
/// </summary>
public static partial class WarManager
{
    internal static readonly object Sync = new();
    private static bool initialized;
    private static long version;
    private static readonly SnowflakeIdGenerator BattleIds = new(7);

    private static readonly Dictionary<int, int[]> Graph = new();
    private static readonly Dictionary<int, WarNodeState> Nodes = new();
    private static readonly Dictionary<int, WarClanInfo> Clans = new();
    /// <summary>NodeId thành chính → ClanId.</summary>
    private static readonly Dictionary<int, int> MainCities = new();
    private static readonly Dictionary<(WarUnitKind, int), WarUnit> Units = new();
    private static readonly Dictionary<long, WarBattleRuntime> Battles = new();
    private static readonly Dictionary<int, IWarSession> Sessions = new();
    /// <summary>Client đã tải xong scene (đã xin snapshot) nên có thể nhận lệnh vào trận ngay.</summary>
    private static readonly HashSet<int> ReadySessions = new();

    // Delta đang gom trong thao tác hiện tại.
    private static readonly Dictionary<int, WarNodeState> pendingNodes = new();
    private static readonly Dictionary<(WarUnitKind, int), WarUnit> pendingUnits = new();
    private static readonly HashSet<(WarUnitKind, int)> pendingRemoved = new();
    private static readonly Dictionary<int, WarClanInfo> pendingClans = new();

    // Ghi bền.
    private static readonly HashSet<int> dirtyNodes = new();
    private static readonly HashSet<int> persistedNodes = new();
    private static readonly HashSet<(WarUnitKind, int)> dirtyUnits = new();
    private static readonly HashSet<int> persistedParticipants = new();
    private static readonly HashSet<int> deletedParticipants = new();
    private static readonly HashSet<int> deletedPuppets = new();
    private static readonly HashSet<long> dirtyBattles = new();
    private static readonly HashSet<long> persistedBattles = new();
    private static readonly Dictionary<long, WarBattle> endedBattlesToSave = new();

    public static bool IsInitialized
    {
        get { lock (Sync) return initialized; }
    }

    // =====================================================================
    // Khởi tạo & khôi phục sau restart
    // =====================================================================

    public static async Task Initialize()
    {
        await using var db = await Program.CreateDbAsync();
        var clanRows = await db.Clans.AsNoTracking()
            .Where(x => x.NodeId > 0)
            .Select(x => new { x.Id, x.Name, x.ImageUrl, x.NodeId, x.Level, x.LeaderCharId })
            .ToListAsync();
        var clans = clanRows.Select(x => new WarClanInfo
        {
            Id = x.Id, Name = x.Name, ImageUrl = x.ImageUrl, MainNodeId = x.NodeId, Level = x.Level,
            LeaderCharId = x.LeaderCharId
        }).ToList();
        var nodes = await db.ClanNodes.AsNoTracking().ToListAsync();
        var participants = await db.WarParticipants.AsNoTracking().ToListAsync();
        var puppets = await db.Puppets.AsNoTracking().ToListAsync();
        var participantIds = participants.Select(x => x.CharId).ToList();
        var names = await db.Characters.AsNoTracking()
            .Where(x => participantIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Progression.Level })
            .ToListAsync();
        var runningBattles = await db.WarBattles
            .Where(x => x.State == WarBattleState.Running)
            .ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var battle in runningBattles)
        {
            battle.State = WarBattleState.Cancelled;
            battle.Outcome = WarBattleOutcome.Draw;
            battle.EndReason = WarBattleEndReason.ServerRestart;
            battle.EndedAt = now;
        }

        if (runningBattles.Count > 0) await db.SaveChangesAsync();

        lock (Sync)
        {
            BuildGraph();
            foreach (var clan in clans)
            {
                Clans[clan.Id] = clan;
                MainCities[clan.MainNodeId] = clan.Id;
            }

            foreach (var map in GameConfig.MapManager.Maps)
            {
                Nodes[map.Id] = new WarNodeState { NodeId = map.Id };
            }

            foreach (var node in nodes)
            {
                persistedNodes.Add(node.NodeId);
                if (!Nodes.TryGetValue(node.NodeId, out var state)) continue;
                state.OwnerClanId = node.ClanId is { } c && Clans.ContainsKey(c) ? c : null;
                state.Version = node.Version;
                if (state.OwnerClanId != node.ClanId) MarkNode(state);
            }

            foreach (var p in participants)
            {
                NormalizeTimes(p);
                persistedParticipants.Add(p.CharId);
                var unit = new WarUnit(p);
                if (!Clans.ContainsKey(p.ClanId))
                {
                    deletedParticipants.Add(p.CharId);
                    continue;
                }

                // Sau restart không còn phiên nào: người đang đi dừng tại node xuất phát của cạnh đang đi,
                // người đang đánh trở về trạng thái đứng yên (trận đã bị huỷ ở trên).
                if (unit.State is WarUnitState.Moving or WarUnitState.Fighting or WarUnitState.Holding)
                {
                    unit.ClearRoute();
                    unit.State = WarUnitState.Idle;
                    MarkUnit(unit);
                }

                Units[unit.Key] = unit;
            }

            foreach (var puppet in puppets)
            {
                NormalizeTimes(puppet);
                if (!Clans.ContainsKey(puppet.ClanId))
                {
                    deletedPuppets.Add(puppet.Id);
                    continue;
                }

                var unit = new WarUnit(puppet);
                if (unit.State == WarUnitState.Fighting || unit.State == WarUnitState.Holding)
                {
                    unit.State = WarUnitState.Idle;
                    MarkUnit(unit);
                }

                Units[unit.Key] = unit;
            }

            // Thành chính luôn thuộc tông của nó.
            foreach (var (nodeId, clanId) in MainCities)
            {
                if (Nodes.TryGetValue(nodeId, out var state) && state.OwnerClanId != clanId)
                {
                    state.OwnerClanId = clanId;
                    MarkNode(state);
                }
            }

            foreach (var n in names) OfflineNames[n.Id] = (n.Name, n.Level);
            initialized = true;
            ClearPendingDelta();
            Log.Information("TongMonChien initialized: {Clans} clans, {Nodes} owned nodes, {Players} players, {Puppets} puppets",
                Clans.Count, Nodes.Values.Count(x => x.OwnerClanId != null), participants.Count, puppets.Count);
        }
    }

    private static void NormalizeTimes(WarParticipant p)
    {
        p.LastHpRecoveryAt = DateTime.SpecifyKind(p.LastHpRecoveryAt, DateTimeKind.Utc);
        p.ArrivedAt = DateTime.SpecifyKind(p.ArrivedAt, DateTimeKind.Utc);
        if (p.NextArrivalAt is { } t) p.NextArrivalAt = DateTime.SpecifyKind(t, DateTimeKind.Utc);
    }

    private static void NormalizeTimes(Puppet p)
    {
        p.LastHpRecoveryAt = DateTime.SpecifyKind(p.LastHpRecoveryAt, DateTimeKind.Utc);
        p.ArrivedAt = DateTime.SpecifyKind(p.ArrivedAt, DateTimeKind.Utc);
        p.CreatedAt = DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc);
        if (p.NextArrivalAt is { } t) p.NextArrivalAt = DateTime.SpecifyKind(t, DateTimeKind.Utc);
    }

    private static void BuildGraph()
    {
        Graph.Clear();
        foreach (var map in GameConfig.MapManager.Maps)
        {
            Graph[map.Id] = map.NextMapIds ?? [];
        }
    }

    private static void EnsureInitialized()
    {
        if (!initialized) throw new InvalidException("Tông môn chiến đang khởi động, hãy thử lại sau.");
    }

    // =====================================================================
    // Tông môn
    // =====================================================================

    /// <summary>Cập nhật/Thêm thông tin tông (gọi khi tạo tông, đổi icon, đổi Leader, lên cấp...).</summary>
    public static void UpsertClan(Models.Clan clan)
    {
        lock (Sync)
        {
            if (!initialized || clan.NodeId <= 0) return;
            if (!Clans.TryGetValue(clan.Id, out var info))
            {
                info = new WarClanInfo { Id = clan.Id };
                Clans[clan.Id] = info;
            }

            if (info.MainNodeId != 0 && info.MainNodeId != clan.NodeId) MainCities.Remove(info.MainNodeId);
            info.Name = clan.Name;
            info.ImageUrl = clan.ImageUrl;
            info.MainNodeId = clan.NodeId;
            info.Level = clan.Level;
            info.LeaderCharId = clan.LeaderCharId;
            MainCities[clan.NodeId] = clan.Id;
            pendingClans[clan.Id] = info;

            // Node vừa thành thành chính: đổi owner, đẩy đối tượng tông khác ở đó về node đồng minh gần nhất.
            if (Nodes.TryGetValue(clan.NodeId, out var node) && node.OwnerClanId != clan.Id)
            {
                SetOwner(node, clan.Id);
                foreach (var unit in UnitsAt(clan.NodeId).Where(x => x.ClanId != clan.Id).ToList())
                {
                    if (unit.State == WarUnitState.Fighting) continue;
                    Relocate(unit, FindNearestAllyNode(unit.ClanId, clan.NodeId));
                }
            }

            PublishDelta();
        }
    }

    /// <summary>Tông bị giải tán: xử thua, trả node về trung lập, xoá rối, đưa người chơi về AllMaps.</summary>
    public static void OnClanDeleted(int clanId)
    {
        List<IWarSession> kicked;
        lock (Sync)
        {
            if (!initialized) return;
            var now = DateTime.UtcNow;
            foreach (var battle in Battles.Values
                         .Where(b => !b.Ended && (b.Attacker.ClanId == clanId || b.Defender.ClanId == clanId)).ToList())
            {
                var loserSide = battle.Attacker.ClanId == clanId ? 0 : 1;
                EndBattle(battle, loserSide == 0 ? WarBattleOutcome.DefenderWin : WarBattleOutcome.AttackerWin,
                    WarBattleEndReason.ClanRemoved, null, null, now, loserRan: true);
            }

            foreach (var node in Nodes.Values.Where(x => x.OwnerClanId == clanId))
            {
                SetOwner(node, null);
            }

            var removedNodes = new HashSet<int>();
            foreach (var unit in Units.Values.Where(x => x.ClanId == clanId).ToList())
            {
                removedNodes.Add(unit.NodeId);
                RemoveUnit(unit);
            }

            if (Clans.Remove(clanId, out var info)) MainCities.Remove(info.MainNodeId);
            kicked = Sessions.Values.Where(s => !Units.ContainsKey((WarUnitKind.Player, s.CharId))).ToList();
            foreach (var s in kicked)
            {
                Sessions.Remove(s.CharId);
                ReadySessions.Remove(s.CharId);
            }
            foreach (var nodeId in removedNodes) ResolveNode(nodeId, now);
            PublishDelta();
        }

        foreach (var s in kicked) s.Kick("Tông môn đã giải tán, bạn rời khỏi Tông môn chiến.");
    }

    /// <summary>Thành viên rời/bị kick khỏi tông: xoá trạng thái chiến của nhân vật.</summary>
    public static void OnMemberRemoved(int clanId, int charId)
    {
        IWarSession? session;
        lock (Sync)
        {
            if (!initialized) return;
            var now = DateTime.UtcNow;
            if (!Units.TryGetValue((WarUnitKind.Player, charId), out var unit) || unit.ClanId != clanId) return;
            if (unit.State == WarUnitState.Fighting && Battles.TryGetValue(unit.BattleId, out var battle) && !battle.Ended)
            {
                var side = battle.SideOf(charId);
                EndBattle(battle, side == 0 ? WarBattleOutcome.DefenderWin : WarBattleOutcome.AttackerWin,
                    WarBattleEndReason.ClanRemoved, null, null, now, loserRan: true);
            }

            var nodeId = unit.NodeId;
            RemoveUnit(unit);
            Sessions.Remove(charId, out session);
            ReadySessions.Remove(charId);
            ResolveNode(nodeId, now);
            PublishDelta();
        }

        session?.Kick("Bạn đã rời tông môn nên không thể tiếp tục Tông môn chiến.");
    }

    // =====================================================================
    // Phiên người chơi
    // =====================================================================

    /// <summary>Người chơi mở scene chiến. Tạo trạng thái chiến lần đầu, đặt lại vị trí nếu node đã mất.</summary>
    public static WarSnapshot Enter(IWarSession session, CharacterData charData, Models.Clan clan, ClanRole role)
    {
        if (clan.NodeId <= 0) throw new InvalidException("Tông môn chưa có thành trên bản đồ nên không thể tham chiến.");
        UpsertClan(clan);
        lock (Sync)
        {
            EnsureInitialized();
            var now = DateTime.UtcNow;
            var charId = charData.Id;
            var view = new CharacterDataView(charId, charData.Progression.Level, charData.Stats.SinhMenh);
            if (!Units.TryGetValue((WarUnitKind.Player, charId), out var unit))
            {
                var max = WarConfig.GetPlayerMaxHp(view);
                var participant = new WarParticipant
                {
                    CharId = charId, ClanId = clan.Id, NodeId = clan.NodeId, LastSafeNodeId = clan.NodeId,
                    HpMax = max, Hp = Math.Min(max, WarConfig.GetInitialPlayerHp(view)),
                    LastHpRecoveryAt = now, ArrivedAt = now, State = WarUnitState.Idle
                };
                unit = new WarUnit(participant);
                Units[unit.Key] = unit;
            }
            else if (unit.ClanId != clan.Id)
            {
                // Đổi tông: bắt đầu lại tại thành chính của tông mới, giữ HP.
                unit.ClanId = clan.Id;
                unit.NodeId = clan.NodeId;
                unit.Player!.LastSafeNodeId = clan.NodeId;
                unit.ClearRoute();
                unit.State = WarUnitState.Idle;
                unit.ArrivedAt = now;
            }

            var newMax = WarConfig.GetPlayerMaxHp(view);
            if (newMax != unit.HpMax)
            {
                unit.HpMax = newMax;
                unit.Hp = Math.Min(unit.Hp, newMax);
            }

            unit.ApplyRegen(now);
            if (unit.State != WarUnitState.Fighting)
            {
                if (unit.State is WarUnitState.Moving or WarUnitState.Holding)
                {
                    unit.ClearRoute();
                }

                unit.State = WarUnitState.Idle;
                if (!IsAllyNode(unit.ClanId, unit.NodeId))
                {
                    Relocate(unit, FindNearestAllyNode(unit.ClanId, unit.NodeId));
                }
            }

            if (Sessions.TryGetValue(charId, out var old) && !ReferenceEquals(old, session))
            {
                Sessions.Remove(charId);
            }

            Sessions[charId] = session;
            ReadySessions.Remove(charId);
            Returning.Remove(charId);
            RememberName(charId, charData.Name, charData.Progression.Level);
            MarkUnit(unit);
            ResolveNode(unit.NodeId, now);
            PublishDelta();
            return BuildSnapshot(charId, clan.Id, role == ClanRole.Leader);
        }
    }

    /// <summary>Người chơi rời scene/mất kết nối. Đang đi thì dừng ở node xuất phát của cạnh đang đi.</summary>
    public static void Leave(IWarSession session)
    {
        lock (Sync)
        {
            if (!initialized) return;
            if (!Sessions.TryGetValue(session.CharId, out var current) || !ReferenceEquals(current, session)) return;
            Sessions.Remove(session.CharId);
            ReadySessions.Remove(session.CharId);
            var now = DateTime.UtcNow;
            if (Units.TryGetValue((WarUnitKind.Player, session.CharId), out var unit) &&
                unit.State != WarUnitState.Fighting)
            {
                if (unit.State is WarUnitState.Moving or WarUnitState.Holding)
                {
                    // Logout giữa đường: về node an toàn ngay trước bước đang đi.
                    if (unit.State == WarUnitState.Moving) unit.NodeId = unit.Player!.LastSafeNodeId;
                    unit.ClearRoute();
                }

                unit.State = WarUnitState.Idle;
                MarkUnit(unit);
                ResolveNode(unit.NodeId, now);
            }

            PublishDelta();
        }
    }

    public static WarSnapshot GetSnapshot(int charId, bool isLeader)
    {
        lock (Sync)
        {
            EnsureInitialized();
            var clanId = Units.TryGetValue((WarUnitKind.Player, charId), out var unit) ? unit.ClanId : 0;
            if (unit != null && Sessions.ContainsKey(charId) && ReadySessions.Add(charId))
            {
                // Client đã sẵn sàng: giờ mới có thể bị ghép trận tại node đang đứng.
                ResolveNode(unit.NodeId, DateTime.UtcNow);
                PublishDelta();
            }

            return BuildSnapshot(charId, clanId, isLeader);
        }
    }

    public static bool IsInScene(int charId)
    {
        lock (Sync) return Sessions.ContainsKey(charId);
    }

    // =====================================================================
    // Truy vấn node
    // =====================================================================

    private static bool IsMainCity(int nodeId) => MainCities.ContainsKey(nodeId);

    private static bool IsOwnMainCity(int clanId, int nodeId) =>
        MainCities.TryGetValue(nodeId, out var owner) && owner == clanId;

    private static bool IsEnemyMainCity(int clanId, int nodeId) =>
        MainCities.TryGetValue(nodeId, out var owner) && owner != clanId;

    private static int? OwnerOf(int nodeId)
    {
        if (MainCities.TryGetValue(nodeId, out var main)) return main;
        return Nodes.TryGetValue(nodeId, out var node) ? node.OwnerClanId : null;
    }

    /// <summary>Node đồng minh: thành chính của tông hoặc node thông thường tông đang sở hữu.</summary>
    private static bool IsAllyNode(int clanId, int nodeId) => OwnerOf(nodeId) == clanId;

    private static int MainCityOf(int clanId) =>
        Clans.TryGetValue(clanId, out var info) ? info.MainNodeId : 0;

    /// <summary>Node đồng minh gần nhất theo số cạnh; không còn node nào thì về thành chính.</summary>
    private static int FindNearestAllyNode(int clanId, int fromNodeId)
    {
        var visited = new HashSet<int> { fromNodeId };
        var queue = new Queue<int>();
        queue.Enqueue(fromNodeId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (IsAllyNode(clanId, current)) return current;
            if (!Graph.TryGetValue(current, out var next)) continue;
            foreach (var n in next)
            {
                if (visited.Add(n)) queue.Enqueue(n);
            }
        }

        return MainCityOf(clanId);
    }

    private static IEnumerable<WarUnit> UnitsAt(int nodeId) =>
        Units.Values.Where(x => x.NodeId == nodeId && x.State != WarUnitState.Moving);

    /// <summary>Người chơi đang ở scene hoặc đang đánh; rối luôn tính. Cần ít nhất 1 HP.</summary>
    private static bool IsPresent(WarUnit unit)
    {
        if (unit.State == WarUnitState.Moving) return false;
        if (unit.State == WarUnitState.Fighting) return true;
        if (unit.Hp < 1) return false;
        return unit.IsPuppet || Sessions.ContainsKey(unit.Id) || Returning.ContainsKey(unit.Id);
    }

    /// <summary>Có thể được ghép trận ngay: rối, hoặc người chơi đang mở scene chiến.</summary>
    private static bool CanBeMatched(WarUnit unit) =>
        unit.State != WarUnitState.Fighting && unit.State != WarUnitState.Moving && unit.Hp >= 1 &&
        (unit.IsPuppet || ReadySessions.Contains(unit.Id));

    /// <summary>
    /// Người chơi vừa thắng/hoà đang ở màn hình kết quả trận: vẫn được tính là đứng ở node (để không
    /// làm node trung lập oan) cho tới khi quay lại scene hoặc hết thời gian chờ.
    /// </summary>
    private static readonly Dictionary<int, DateTime> Returning = new();
    private static readonly TimeSpan ReturnGrace = TimeSpan.FromSeconds(90);

    // =====================================================================
    // Thay đổi trạng thái (ghi delta + đánh dấu cần lưu)
    // =====================================================================

    private static void SetOwner(WarNodeState node, int? clanId)
    {
        if (node.OwnerClanId == clanId) return;
        if (clanId == null && MainCities.ContainsKey(node.NodeId)) return;
        Log.Information("TongMonChien owner node {NodeId}: {Old} -> {New}", node.NodeId, node.OwnerClanId, clanId);
        node.OwnerClanId = clanId;
        node.Version++;
        MarkNode(node);
    }

    private static void MarkNode(WarNodeState node)
    {
        dirtyNodes.Add(node.NodeId);
        pendingNodes[node.NodeId] = node;
    }

    private static void MarkUnit(WarUnit unit)
    {
        dirtyUnits.Add(unit.Key);
        pendingUnits[unit.Key] = unit;
    }

    private static void RemoveUnit(WarUnit unit)
    {
        Units.Remove(unit.Key);
        pendingUnits.Remove(unit.Key);
        pendingRemoved.Add(unit.Key);
        dirtyUnits.Remove(unit.Key);
        if (unit.IsPuppet) deletedPuppets.Add(unit.Id);
        else deletedParticipants.Add(unit.Id);
    }

    /// <summary>Dịch chuyển tức thời (thua/bỏ chạy/node đổi chủ), huỷ hành trình.</summary>
    private static void Relocate(WarUnit unit, int nodeId)
    {
        unit.ClearRoute();
        unit.State = WarUnitState.Idle;
        unit.NodeId = nodeId;
        unit.ArrivedAt = DateTime.UtcNow;
        if (unit.Player != null) unit.Player.LastSafeNodeId = nodeId;
        MarkUnit(unit);
    }

    // =====================================================================
    // Snapshot / delta
    // =====================================================================

    private static WarSnapshot BuildSnapshot(int charId, int clanId, bool isLeader)
    {
        var now = DateTime.UtcNow;
        var clanLevel = Clans.TryGetValue(clanId, out var info) ? info.Level : 1;
        return new WarSnapshot
        {
            Success = true,
            Version = version,
            SecondsPerEdge = WarConfig.SecondsPerEdge,
            ServerTime = now,
            MyCharId = charId,
            MyClanId = clanId,
            IsLeader = isLeader,
            MaxPuppetCount = WarConfig.GetMaxPuppetCount(clanLevel),
            MaxPuppetLevel = WarConfig.GetMaxPuppetLevel(clanLevel),
            Clans = Clans.Values.Select(ToView).ToArray(),
            Nodes = Nodes.Values.Where(x => x.OwnerClanId != null || IsFighting(x.NodeId))
                .Select(ToView).ToArray(),
            Units = Units.Values.Select(u => ToView(u, now)).ToArray()
        };
    }

    private static bool IsFighting(int nodeId) => Battles.Values.Any(b => !b.Ended && b.NodeId == nodeId);

    private static WarClanView ToView(WarClanInfo c) => new()
    {
        ClanId = c.Id, Name = c.Name, ImageUrl = c.ImageUrl, MainNodeId = c.MainNodeId
    };

    private static WarNodeView ToView(WarNodeState n) => new()
    {
        NodeId = n.NodeId, OwnerClanId = OwnerOf(n.NodeId), IsFighting = IsFighting(n.NodeId)
    };

    internal static WarUnitView ToView(WarUnit u, DateTime now)
    {
        var view = new WarUnitView
        {
            Kind = u.Kind,
            Id = u.Id,
            ClanId = u.ClanId,
            NodeId = u.NodeId,
            Hp = u.Hp,
            HpMax = u.HpMax,
            RegenPerMinute = u.State == WarUnitState.Fighting ? 0 : u.RegenPerMinute,
            HpUpdatedAt = u.LastHpRecoveryAt,
            State = u.State,
            Route = u.Route.ToArray(),
            NextArrivalAt = u.NextArrivalAt,
            IsActive = u.IsPuppet || Sessions.ContainsKey(u.Id)
        };
        if (u.IsPuppet)
        {
            view.Name = $"Khôi Lỗi #{u.Id}";
            view.Level = u.Puppet!.Level;
        }
        else if (Sessions.TryGetValue(u.Id, out var session) && session.Client.CharacterData is { } c)
        {
            view.Name = c.Name;
            view.Level = c.Progression.Level;
            view.Gender = c.Gender;
            view.Appearance = c.Appearance;
        }
        else
        {
            view.Name = OfflineNames.TryGetValue(u.Id, out var n) ? n.Name : string.Empty;
            view.Level = OfflineNames.TryGetValue(u.Id, out n) ? n.Level : 0;
        }

        return view;
    }

    /// <summary>Tên/level người chơi đã từng vào chiến, dùng hiển thị khi họ offline.</summary>
    private static readonly Dictionary<int, (string Name, int Level)> OfflineNames = new();

    internal static void RememberName(int charId, string name, int level)
    {
        OfflineNames[charId] = (name, level);
    }

    private static void ClearPendingDelta()
    {
        pendingNodes.Clear();
        pendingUnits.Clear();
        pendingRemoved.Clear();
        pendingClans.Clear();
    }

    /// <summary>Gửi các thay đổi đã gom thành một delta có version kế tiếp cho mọi người ở scene chiến.</summary>
    private static void PublishDelta()
    {
        if (pendingNodes.Count == 0 && pendingUnits.Count == 0 && pendingRemoved.Count == 0 && pendingClans.Count == 0)
            return;
        var now = DateTime.UtcNow;
        version++;
        var delta = new WarDelta
        {
            Version = version,
            ServerTime = now,
            Clans = pendingClans.Values.Select(ToView).ToArray(),
            Nodes = pendingNodes.Values.Select(ToView).ToArray(),
            Units = pendingUnits.Values.Select(u => ToView(u, now)).ToArray(),
            RemovedUnits = pendingRemoved.Select(k => new WarUnitRef { Kind = k.Item1, Id = k.Item2 }).ToArray()
        };
        ClearPendingDelta();
        foreach (var session in Sessions.Values)
        {
            try
            {
                session.MemSend(MessageTag.WarDelta, delta);
            }
            catch (Exception e)
            {
                Log.Warning(e, "TongMonChien send delta failed; CharId={CharId}", session.CharId);
            }
        }
    }

    // =====================================================================
    // Ghi bền
    // =====================================================================

    /// <summary>Ghi các bản ghi đã đổi xuống DB. Gọi định kỳ và khi dừng server.</summary>
    public static async Task Flush()
    {
        List<ClanNode> nodes;
        List<WarParticipant> participants;
        List<Puppet> puppets;
        List<int> removedParticipants;
        List<int> removedPuppets;
        List<WarBattle> battles;
        HashSet<int> knownNodes;
        HashSet<int> knownParticipants;
        HashSet<long> knownBattles;
        lock (Sync)
        {
            if (!initialized) return;
            var now = DateTime.UtcNow;
            nodes = dirtyNodes.Where(Nodes.ContainsKey).Select(id => Nodes[id]).Select(n => new ClanNode
            {
                NodeId = n.NodeId, ClanId = MainCities.ContainsKey(n.NodeId) ? null : n.OwnerClanId,
                Version = n.Version, UpdatedAt = now
            }).ToList();
            participants = dirtyUnits.Where(k => k.Item1 == WarUnitKind.Player && Units.ContainsKey(k))
                .Select(k => Clone(Units[k].Player!)).ToList();
            puppets = dirtyUnits.Where(k => k.Item1 == WarUnitKind.Puppet && Units.ContainsKey(k))
                .Select(k => Clone(Units[k].Puppet!)).ToList();
            removedParticipants = deletedParticipants.ToList();
            removedPuppets = deletedPuppets.ToList();
            battles = dirtyBattles.Select(id => Battles.TryGetValue(id, out var b) ? b.Record : endedBattlesToSave.GetValueOrDefault(id))
                .Where(x => x != null).Select(x => Clone(x!)).ToList();
            dirtyNodes.Clear();
            dirtyUnits.Clear();
            deletedParticipants.Clear();
            deletedPuppets.Clear();
            dirtyBattles.Clear();
            endedBattlesToSave.Clear();
            knownNodes = new HashSet<int>(persistedNodes);
            knownParticipants = new HashSet<int>(persistedParticipants);
            knownBattles = new HashSet<long>(persistedBattles);
        }

        if (nodes.Count + participants.Count + puppets.Count + removedParticipants.Count + removedPuppets.Count +
            battles.Count == 0) return;
        try
        {
            await using var db = await Program.CreateDbAsync();
            foreach (var n in nodes)
            {
                if (knownNodes.Contains(n.NodeId)) db.ClanNodes.Update(n);
                else db.ClanNodes.Add(n);
            }

            foreach (var p in participants)
            {
                if (knownParticipants.Contains(p.CharId)) db.WarParticipants.Update(p);
                else db.WarParticipants.Add(p);
            }

            foreach (var p in puppets) db.Puppets.Update(p);
            foreach (var b in battles)
            {
                if (knownBattles.Contains(b.Id)) db.WarBattles.Update(b);
                else db.WarBattles.Add(b);
            }

            await db.SaveChangesAsync();
            if (removedParticipants.Count > 0)
                await db.WarParticipants.Where(x => removedParticipants.Contains(x.CharId)).ExecuteDeleteAsync();
            if (removedPuppets.Count > 0)
                await db.Puppets.Where(x => removedPuppets.Contains(x.Id)).ExecuteDeleteAsync();
            lock (Sync)
            {
                foreach (var n in nodes) persistedNodes.Add(n.NodeId);
                foreach (var p in participants) persistedParticipants.Add(p.CharId);
                foreach (var p in removedParticipants) persistedParticipants.Remove(p);
                foreach (var b in battles)
                {
                    if (b.State == WarBattleState.Running) persistedBattles.Add(b.Id);
                    else persistedBattles.Remove(b.Id);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "TongMonChien flush failed; retry next tick");
            lock (Sync)
            {
                foreach (var n in nodes) dirtyNodes.Add(n.NodeId);
                foreach (var p in participants) dirtyUnits.Add((WarUnitKind.Player, p.CharId));
                foreach (var p in puppets) dirtyUnits.Add((WarUnitKind.Puppet, p.Id));
                foreach (var p in removedParticipants) deletedParticipants.Add(p);
                foreach (var p in removedPuppets) deletedPuppets.Add(p);
                foreach (var b in battles)
                {
                    dirtyBattles.Add(b.Id);
                    if (!Battles.ContainsKey(b.Id)) endedBattlesToSave[b.Id] = b;
                }
            }
        }
    }

    private static WarParticipant Clone(WarParticipant p) => new()
    {
        CharId = p.CharId, ClanId = p.ClanId, NodeId = p.NodeId, Hp = p.Hp, HpMax = p.HpMax,
        LastHpRecoveryAt = p.LastHpRecoveryAt, State = p.State, Route = p.Route, NextArrivalAt = p.NextArrivalAt,
        LastSafeNodeId = p.LastSafeNodeId, ArrivedAt = p.ArrivedAt
    };

    private static Puppet Clone(Puppet p) => new()
    {
        Id = p.Id, ClanId = p.ClanId, Level = p.Level, RandomSeed = p.RandomSeed, NodeId = p.NodeId, Hp = p.Hp,
        HpMax = p.HpMax, LastHpRecoveryAt = p.LastHpRecoveryAt, State = p.State, Route = p.Route,
        NextArrivalAt = p.NextArrivalAt, ArrivedAt = p.ArrivedAt, CreatedAt = p.CreatedAt
    };

    private static WarBattle Clone(WarBattle b) => new()
    {
        Id = b.Id, NodeId = b.NodeId, IsPvp = b.IsPvp, AttackerCharId = b.AttackerCharId,
        AttackerClanId = b.AttackerClanId, DefenderCharId = b.DefenderCharId, DefenderPuppetId = b.DefenderPuppetId,
        DefenderClanId = b.DefenderClanId, AttackerHpBefore = b.AttackerHpBefore,
        DefenderHpBefore = b.DefenderHpBefore, AttackerHpAfter = b.AttackerHpAfter,
        DefenderHpAfter = b.DefenderHpAfter, State = b.State, Outcome = b.Outcome, EndReason = b.EndReason,
        CreatedAt = b.CreatedAt, EndedAt = b.EndedAt
    };
}
