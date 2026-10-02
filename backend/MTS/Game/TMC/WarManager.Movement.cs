using MTS.Exceptions;
using Serilog;

namespace MTS.Game.TMC;

public static partial class WarManager
{
    /// <summary>Độn tốc hiện tại của đối tượng: rối theo giá trị đã chốt, người chơi theo chỉ số nhân vật đang online.</summary>
    private static int DonTocOf(WarUnit unit) =>
        unit.IsPuppet
            ? unit.Puppet!.DonToc
            : Sessions.TryGetValue(unit.Id, out var session) && session.Client.CharacterData is { } c
                ? c.Stats.TocDoBay
                : 0;

    /// <summary>Thời gian đi một cạnh của đối tượng (10 giây, mỗi 2000 độn tốc giảm 1 giây, tối thiểu 1 giây).</summary>
    private static TimeSpan EdgeDurationOf(WarUnit unit) =>
        TimeSpan.FromSeconds(WarConfig.GetSecondsPerEdge(DonTocOf(unit)));

    /// <summary>Bắt đầu đi một cạnh mới từ thời điểm <paramref name="start"/>: chốt thời gian đi cạnh và mốc tới node kế.</summary>
    private static void BeginEdge(WarUnit unit, DateTime start)
    {
        var duration = EdgeDurationOf(unit);
        unit.ClearEdge();
        unit.EdgeSeconds = duration.TotalSeconds;
        unit.NextArrivalAt = start + duration;
    }

    /// <summary>Người chơi chọn node đích; server tìm route hợp lệ và bắt đầu đi. Không có lệnh huỷ.</summary>
    public static int[] Move(int charId, int targetNodeId)
    {
        lock (Sync)
        {
            EnsureInitialized();
            if (!Sessions.ContainsKey(charId)) throw new InvalidException("Bạn chưa vào Tông môn chiến.");
            if (!Units.TryGetValue((WarUnitKind.Player, charId), out var unit))
                throw new InvalidException("Bạn chưa vào Tông môn chiến.");
            var now = DateTime.UtcNow;
            if (unit.ApplyRegen(now)) MarkUnit(unit);
            switch (unit.State)
            {
                case WarUnitState.Moving: throw new InvalidException("Đang di chuyển, không thể đổi lộ trình.");
                case WarUnitState.Fighting: throw new InvalidException("Đang giao tranh.");
                case WarUnitState.Holding: throw new InvalidException("Node hiện tại đang giao tranh, hãy chờ phân thắng bại.");
            }

            if (!WarConfig.CanMove(unit.Hp, unit.HpMax))
                throw new InvalidException(WarConfig.MoveHpMessage("Bạn") + " Hãy chờ hồi phục hoặc dùng đan.");
            if (!Nodes.ContainsKey(targetNodeId)) throw new InvalidException("Node không tồn tại.");
            if (targetNodeId == unit.NodeId) throw new InvalidException("Bạn đang ở node này.");
            if (IsEnemyMainCity(unit.ClanId, targetNodeId))
                throw new InvalidException("Không thể dừng hay chiếm thành chính của tông khác.");
            var route = FindPlayerRoute(unit.ClanId, unit.NodeId, targetNodeId)
                        ?? throw new InvalidException("Không có đường đi hợp lệ (không thể đi xuyên node của tông khác).");
            StartMoving(unit, route, now);
            PublishDelta();
            return route.ToArray();
        }
    }

    /// <summary>
    /// BFS: node trung gian phải là node đồng minh, node trung lập (dừng lại đánh rối trung lập, thắng thì chiếm) hoặc thành chính địch
    /// (chỉ đi xuyên). Node đích là bất kỳ node nào trừ thành chính địch.
    /// </summary>
    private static List<int>? FindPlayerRoute(int clanId, int start, int target)
    {
        bool CanPass(int node) =>
            IsAllyNode(clanId, node) || OwnerOf(node) == null || IsEnemyMainCity(clanId, node);

        return Bfs(start, target, CanPass, node => !IsEnemyMainCity(clanId, node));
    }

    /// <summary>Rối chỉ đi qua và dừng ở node tông mình đang sở hữu (kể cả thành chính).</summary>
    private static List<int>? FindPuppetRoute(int clanId, int start, int target) =>
        Bfs(start, target, node => IsAllyNode(clanId, node), node => IsAllyNode(clanId, node));

    private static List<int>? Bfs(int start, int target, Func<int, bool> canPass, Func<int, bool> canStop)
    {
        if (!canStop(target)) return null;
        var parent = new Dictionary<int, int> { [start] = start };
        var queue = new Queue<int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target) break;
            if (current != start && !canPass(current)) continue;
            if (!Graph.TryGetValue(current, out var next)) continue;
            foreach (var n in next)
            {
                if (parent.ContainsKey(n)) continue;
                parent[n] = current;
                queue.Enqueue(n);
            }
        }

        if (!parent.ContainsKey(target)) return null;
        var path = new List<int>();
        for (var node = target; node != start; node = parent[node]) path.Add(node);
        path.Reverse();
        return path;
    }

    private static void StartMoving(WarUnit unit, List<int> route, DateTime now)
    {
        var from = unit.NodeId;
        unit.SetRoute(route);
        unit.State = WarUnitState.Moving;
        BeginEdge(unit, now);
        if (unit.Player != null) unit.Player.LastSafeNodeId = from;
        MarkUnit(unit);
        ResolveNode(from, now);
    }

    /// <summary>Xử lý đến node, hết hạn trận, hết thời gian chờ quay lại scene. Gọi mỗi ~200ms.</summary>
    public static void Tick()
    {
        lock (Sync)
        {
            if (!initialized) return;
            var now = DateTime.UtcNow;
            // Có thể một đối tượng đến nhiều node trong một tick nếu server bị trễ; lặp tới khi hết.
            for (var guard = 0; guard < 64; guard++)
            {
                var arrivals = Units.Values
                    .Where(x => x.State == WarUnitState.Moving && x.NextArrivalAt <= now)
                    .OrderBy(x => x.NextArrivalAt)
                    .ThenBy(x => x.IsPuppet ? 1 : 0)
                    .ThenBy(x => x.Id)
                    .ToList();
                // Gặp nhau giữa đường xảy ra trước khi tới node: xét trước, ai vào trận thì không còn "đang đi".
                MatchOnEdges(now);
                arrivals = arrivals.Where(x => x.State == WarUnitState.Moving).ToList();
                if (arrivals.Count == 0) break;
                foreach (var unit in arrivals)
                {
                    if (unit.State != WarUnitState.Moving) continue;
                    ProcessArrival(unit, unit.NextArrivalAt ?? now, now);
                }
            }

            TickBattles(now);
            foreach (var (charId, until) in Returning.Where(x => x.Value <= now).ToList())
            {
                Returning.Remove(charId);
                // Người vừa thắng trận giữa đường đang đi nốt cạnh: để đi tới node kế rồi mới dừng.
                if (Units.TryGetValue((WarUnitKind.Player, charId), out var unit) && unit.State != WarUnitState.Fighting &&
                    !(unit.State == WarUnitState.Moving && unit.EdgeResume))
                {
                    unit.ClearRoute();
                    unit.State = WarUnitState.Idle;
                    MarkUnit(unit);
                    ResolveNode(unit.NodeId, now);
                }
            }

            PublishDelta();
        }
    }

    private static void ProcessArrival(WarUnit unit, DateTime arrivalTime, DateTime now)
    {
        if (unit.Route.Count == 0)
        {
            unit.State = WarUnitState.Idle;
            unit.NextArrivalAt = null;
            MarkUnit(unit);
            return;
        }

        var clanId = unit.ClanId;
        var next = unit.Route[0];
        var isLast = unit.Route.Count == 1;
        if (unit.IsPuppet)
        {
            // Node kế tiếp đã mất: rối dừng tại node đã đến gần nhất.
            if (!IsAllyNode(clanId, next))
            {
                unit.ClearRoute();
                unit.State = WarUnitState.Idle;
                MarkUnit(unit);
                ResolveNode(unit.NodeId, now);
                return;
            }
        }
        else
        {
            // Kiểm lại quyền đi ở từng bước: node trung gian vừa bị địch chiếm thì dừng lại đánh node đó.
            var passable = IsAllyNode(clanId, next) || OwnerOf(next) == null || IsEnemyMainCity(clanId, next);
            if (!isLast && !passable)
            {
                unit.SetRoute(new[] { next });
                isLast = true;
            }

            if (isLast && IsEnemyMainCity(clanId, next))
            {
                // Node đích vừa trở thành thành chính địch: không được dừng ở đó.
                unit.ClearRoute();
                unit.State = WarUnitState.Idle;
                MarkUnit(unit);
                ResolveNode(unit.NodeId, now);
                return;
            }
        }

        unit.NodeId = next;
        unit.ArrivedAt = arrivalTime;
        unit.SetRoute(unit.Route.Skip(1));
        if (IsEnemyMainCity(clanId, next))
        {
            // Đi xuyên thành chính địch: không dừng, không chiếm, không bị chặn.
            BeginEdge(unit, arrivalTime);
            MarkUnit(unit);
            return;
        }

        if (unit.Player != null) unit.Player.LastSafeNodeId = next;
        unit.NextArrivalAt = null;
        unit.ClearEdge();
        unit.State = WarUnitState.Holding;
        MarkUnit(unit);
        Log.Debug("TongMonChien arrive {Kind} {Id} node {NodeId}", unit.Kind, unit.Id, next);
        ResolveNode(next, now);
        // Người đi nốt cạnh sau trận giữa đường mà chưa quay lại scene: tới node thì đứng yên chờ họ vào lại.
        if (!unit.IsPuppet && unit.State == WarUnitState.Holding && !Sessions.ContainsKey(unit.Id) &&
            !Returning.ContainsKey(unit.Id))
        {
            unit.ClearRoute();
            unit.State = WarUnitState.Idle;
            MarkUnit(unit);
        }
    }

    // =====================================================================
    // Gặp nhau giữa đường
    // =====================================================================

    /// <summary>Vị trí trên cạnh tính từ node có Id nhỏ hơn (0..1) và vận tốc (phần cạnh mỗi giây, có dấu theo chiều đi).</summary>
    private static (double X, double V) EdgeMotion(WarUnit unit, DateTime at)
    {
        var from = unit.NodeId;
        var to = unit.Route[0];
        // Không kẹp về [0,1]: đối tượng đã quá giờ tới node (chờ xử lý trong tick này) vẫn nội suy tuyến tính đúng.
        var progress = 1 - (unit.NextArrivalAt!.Value - at).TotalSeconds / unit.EdgeSeconds;
        var speed = 1 / unit.EdgeSeconds;
        return from < to ? (progress, speed) : (1 - progress, -speed);
    }

    /// <summary>
    /// Hai đơn vị khác tông đang đi trên cùng một cạnh mà gặp nhau (đi ngược chiều, hoặc cùng chiều và bị đuổi kịp)
    /// thì ghép trận ngay giữa đường. Rối không đánh rối. Mỗi cặp chỉ ghép một lần trên một cạnh.
    /// Người thắng/hoà đi nốt cạnh; người thua về thành chính.
    /// </summary>
    private static void MatchOnEdges(DateTime now)
    {
        var movers = Units.Values
            .Where(x => x.State == WarUnitState.Moving && x.Route.Count > 0 && x.NextArrivalAt != null &&
                        x.EdgeSeconds > 0 && x.Hp >= 1 && (x.IsPuppet || ReadySessions.Contains(x.Id)))
            .GroupBy(x => (Math.Min(x.NodeId, x.Route[0]), Math.Max(x.NodeId, x.Route[0])))
            .Where(g => g.Count() >= 2 && g.Select(x => x.ClanId).Distinct().Count() >= 2);
        var meetings = new List<(DateTime At, WarUnit A, WarUnit B)>();
        foreach (var group in movers)
        {
            var list = group.OrderBy(x => x.IsPuppet ? 1 : 0).ThenBy(x => x.Id).ToList();
            for (var i = 0; i < list.Count; i++)
            for (var j = i + 1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];
                if (a.ClanId == b.ClanId || (a.IsPuppet && b.IsPuppet) || a.EdgePassed.Contains(b.Key)) continue;
                var (xa, va) = EdgeMotion(a, now);
                var (xb, vb) = EdgeMotion(b, now);
                if (Math.Abs(va - vb) < 1e-9) continue;
                // Thời điểm hai vị trí trùng nhau (âm = đã gặp trước "now").
                var seconds = (xb - xa) / (va - vb);
                if (seconds > 0) continue;
                var at = now.AddSeconds(seconds);
                // Cả hai phải đang ở trên cạnh lúc gặp (sau khi cả hai xuất phát).
                var startA = a.NextArrivalAt!.Value.AddSeconds(-a.EdgeSeconds);
                var startB = b.NextArrivalAt!.Value.AddSeconds(-b.EdgeSeconds);
                if (at < startA || at < startB) continue;
                // Một bên đã tới node trước khi kịp gặp: không tính là gặp giữa đường (sẽ xử lý tại node).
                if (at > a.NextArrivalAt.Value || at > b.NextArrivalAt.Value) continue;
                meetings.Add((at, a, b));
            }
        }

        foreach (var (at, a, b) in meetings.OrderBy(x => x.At))
        {
            if (a.State != WarUnitState.Moving || b.State != WarUnitState.Moving) continue;
            // Rối luôn là bên phòng thủ; giữa hai người chơi, người xuất phát sau là bên tấn công.
            var attacker = a.IsPuppet ? b : b.IsPuppet ? a
                : a.NextArrivalAt!.Value.AddSeconds(-a.EdgeSeconds) >= b.NextArrivalAt!.Value.AddSeconds(-b.EdgeSeconds) ? a : b;
            var defender = ReferenceEquals(attacker, a) ? b : a;
            var remainingA = a.NextArrivalAt!.Value - at;
            var remainingB = b.NextArrivalAt!.Value - at;
            a.EdgePassed.Add(b.Key);
            b.EdgePassed.Add(a.Key);
            StartBattle(attacker.NodeId, attacker, defender, now, onEdge: true);
            if (a.State != WarUnitState.Fighting || b.State != WarUnitState.Fighting) continue;
            // Dừng cả hai tại chỗ gặp nhau: nhớ phần đường còn lại để đi nốt sau trận.
            a.EdgeRemaining = remainingA < TimeSpan.Zero ? TimeSpan.Zero : remainingA;
            b.EdgeRemaining = remainingB < TimeSpan.Zero ? TimeSpan.Zero : remainingB;
            MarkUnit(a);
            MarkUnit(b);
            Log.Information("TongMonChien edge battle {AKind} {AId} vs {BKind} {BId} on edge {From}-{To}",
                a.Kind, a.Id, b.Kind, b.Id, a.NodeId, a.Route[0]);
        }
    }

    /// <summary>
    /// Luật "ai còn đứng ở node": ≥ 2 phe → ghép trận FIFO; còn trận → chờ; đúng 1 phe → phe đó giữ/chiếm;
    /// không còn ai sau giao tranh → node thông thường về trung lập.
    /// </summary>
    private static void ResolveNode(int nodeId, DateTime now)
    {
        if (!Nodes.TryGetValue(nodeId, out var node)) return;
        if (IsMainCity(nodeId))
        {
            ContinueHolders(nodeId, MainCities[nodeId], now);
            return;
        }

        var present = UnitsAt(nodeId).Where(IsPresent).ToList();
        var clans = present.Select(x => x.ClanId).Distinct().ToList();
        if (clans.Count >= 2)
        {
            node.Contested = true;
            MatchAt(nodeId, present, now);
            foreach (var unit in present.Where(x => x.State == WarUnitState.Idle))
            {
                unit.State = WarUnitState.Holding;
                MarkUnit(unit);
            }

            return;
        }

        if (Battles.Values.Any(b => !b.Ended && b.NodeId == nodeId)) return;
        if (clans.Count == 1 && clans[0] != WarConfig.NeutralClanId)
        {
            var captured = node.OwnerClanId != clans[0];
            SetOwner(node, clans[0]);
            node.Contested = false;
            // Thành tựu "chiếm điểm/thành": tính cho người chơi trực tiếp có mặt lúc node về tay tông mình.
            if (captured) CreditCapture(nodeId, present);
            ContinueHolders(nodeId, clans[0], now);
        }
        else if (clans.Count == 1)
        {
            // Chỉ còn rối trung lập: thành vẫn trung lập, rối đứng canh tiếp.
            node.Contested = false;
            foreach (var unit in present.Where(x => x.State == WarUnitState.Holding))
            {
                unit.State = WarUnitState.Idle;
                MarkUnit(unit);
            }
        }
        else if (node.Contested)
        {
            node.Contested = false;
            SetOwner(node, null);
            EnsureNeutralPuppet(nodeId, now);
        }
    }

    /// <summary>Node đã phân định: đối tượng đang chờ của phe giữ node đi tiếp route, còn lại đứng yên.</summary>
    private static void ContinueHolders(int nodeId, int ownerClanId, DateTime now)
    {
        foreach (var unit in UnitsAt(nodeId).Where(x => x.State == WarUnitState.Holding).ToList())
        {
            // Sau giao tranh, HP dưới 99% thì dừng lại ở node này (phải hồi đủ mới đi tiếp).
            if (unit.ClanId == ownerClanId && unit.Route.Count > 0 && WarConfig.CanMove(unit.Hp, unit.HpMax))
            {
                unit.State = WarUnitState.Moving;
                BeginEdge(unit, now);
                if (unit.Player != null) unit.Player.LastSafeNodeId = nodeId;
            }
            else
            {
                unit.ClearRoute();
                unit.State = WarUnitState.Idle;
            }

            MarkUnit(unit);
        }
    }

    /// <summary>Ghép 1v1 theo thứ tự đến node (cùng lúc: người trước rối, rồi ID tăng dần). Rối không đánh rối.</summary>
    private static void MatchAt(int nodeId, List<WarUnit> present, DateTime now)
    {
        var free = present.Where(CanBeMatched)
            .OrderBy(x => x.ArrivedAt)
            .ThenBy(x => x.IsPuppet ? 1 : 0)
            .ThenBy(x => x.Id)
            .ToList();
        var matched = new HashSet<WarUnit>();
        foreach (var u in free)
        {
            if (matched.Contains(u)) continue;
            var v = free.FirstOrDefault(x =>
                !ReferenceEquals(x, u) && !matched.Contains(x) && x.ClanId != u.ClanId && !(x.IsPuppet && u.IsPuppet));
            if (v == null) continue;
            matched.Add(u);
            matched.Add(v);
            // Người đến sau là bên tấn công; trận với rối thì người chơi luôn là bên tấn công.
            var attacker = u.IsPuppet ? v : v.IsPuppet ? u : v;
            var defender = ReferenceEquals(attacker, u) ? v : u;
            StartBattle(nodeId, attacker, defender, now);
        }
    }
}
