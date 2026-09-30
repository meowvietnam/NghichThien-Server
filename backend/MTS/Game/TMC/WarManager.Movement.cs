using MTS.Exceptions;
using Serilog;

namespace MTS.Game.TMC;

public static partial class WarManager
{
    private static TimeSpan EdgeDuration => TimeSpan.FromSeconds(WarConfig.SecondsPerEdge);

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

            if (unit.Hp < 1) throw new InvalidException("HP chiến đã cạn, hãy hồi phục (theo thời gian hoặc dùng đan).");
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
    /// BFS: node trung gian phải là node đồng minh, node trung lập (sẽ chiếm khi đi qua) hoặc thành chính địch
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
        unit.NextArrivalAt = now + EdgeDuration;
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
                if (Units.TryGetValue((WarUnitKind.Player, charId), out var unit) && unit.State != WarUnitState.Fighting)
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
            unit.NextArrivalAt = arrivalTime + EdgeDuration;
            MarkUnit(unit);
            return;
        }

        if (unit.Player != null) unit.Player.LastSafeNodeId = next;
        unit.NextArrivalAt = null;
        unit.State = WarUnitState.Holding;
        MarkUnit(unit);
        Log.Debug("TongMonChien arrive {Kind} {Id} node {NodeId}", unit.Kind, unit.Id, next);
        ResolveNode(next, now);
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
        if (clans.Count == 1)
        {
            SetOwner(node, clans[0]);
            node.Contested = false;
            ContinueHolders(nodeId, clans[0], now);
        }
        else if (node.Contested)
        {
            node.Contested = false;
            SetOwner(node, null);
        }
    }

    /// <summary>Node đã phân định: đối tượng đang chờ của phe giữ node đi tiếp route, còn lại đứng yên.</summary>
    private static void ContinueHolders(int nodeId, int ownerClanId, DateTime now)
    {
        foreach (var unit in UnitsAt(nodeId).Where(x => x.State == WarUnitState.Holding).ToList())
        {
            if (unit.ClanId == ownerClanId && unit.Route.Count > 0 && unit.Hp >= 1)
            {
                unit.State = WarUnitState.Moving;
                unit.NextArrivalAt = now + EdgeDuration;
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
