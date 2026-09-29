using MTS.Game.Combat;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.TMC;

public static partial class WarManager
{
    /// <summary>Kết quả trận gần đây theo (BattleId, CharId) để trả lại khi client báo lặp.</summary>
    private static readonly Dictionary<(long, int), WarBattleEnded> EndedResults = new();
    private static readonly Queue<(long, int)> EndedResultOrder = new();

    private static void StartBattle(int nodeId, WarUnit attacker, WarUnit defender, DateTime now)
    {
        attacker.ApplyRegen(now);
        defender.ApplyRegen(now);
        var isPvp = !defender.IsPuppet;
        var record = new WarBattle
        {
            Id = BattleIds.NextId(),
            NodeId = nodeId,
            IsPvp = isPvp,
            AttackerCharId = attacker.Id,
            AttackerClanId = attacker.ClanId,
            DefenderCharId = isPvp ? defender.Id : null,
            DefenderPuppetId = isPvp ? null : defender.Id,
            DefenderClanId = defender.ClanId,
            AttackerHpBefore = attacker.Hp,
            DefenderHpBefore = defender.Hp,
            AttackerHpAfter = attacker.Hp,
            DefenderHpAfter = defender.Hp,
            State = WarBattleState.Running,
            CreatedAt = now
        };

        CharacterSnapshot attackerSnapshot;
        CharacterSnapshot defenderSnapshot;
        if (!Sessions.TryGetValue(attacker.Id, out var attackerSession) ||
            (isPvp && !Sessions.TryGetValue(defender.Id, out _)))
        {
            return;
        }

        try
        {
            attackerSnapshot = BuildPlayerSnapshot(attackerSession, attacker);
            defenderSnapshot = isPvp
                ? BuildPlayerSnapshot(Sessions[defender.Id], defender)
                : BuildPuppetSnapshot(defender);
        }
        catch (Exception e)
        {
            Log.Error(e, "TongMonChien build battle snapshot failed; node {NodeId}", nodeId);
            return;
        }

        var battle = new WarBattleRuntime { Record = record, Attacker = attacker, Defender = defender };
        Battles[battle.Id] = battle;
        dirtyBattles.Add(battle.Id);
        foreach (var unit in new[] { attacker, defender })
        {
            unit.State = WarUnitState.Fighting;
            unit.BattleId = battle.Id;
            unit.LastHpRecoveryAt = now;
            MarkUnit(unit);
        }

        if (Nodes.TryGetValue(nodeId, out var node)) pendingNodes[nodeId] = node;
        Log.Information(
            "TongMonChien battle {BattleId} start node {NodeId}: {AKind} {AId} (clan {AClan}, hp {AHp}) vs {DKind} {DId} (clan {DClan}, hp {DHp})",
            battle.Id, nodeId, attacker.Kind, attacker.Id, attacker.ClanId, attacker.Hp, defender.Kind, defender.Id,
            defender.ClanId, defender.Hp);

        var toAttacker = new WarBattleMatched
        {
            BattleId = battle.Id, NodeId = nodeId, IsPvp = isPvp, EnemyIsPuppet = !isPvp,
            EnemyPuppetId = isPvp ? 0 : defender.Id, Me = attackerSnapshot, Enemy = defenderSnapshot
        };
        attackerSession.StartCombat(toAttacker);
        if (isPvp)
        {
            var toDefender = new WarBattleMatched
            {
                BattleId = battle.Id, NodeId = nodeId, IsPvp = true, Me = defenderSnapshot, Enemy = attackerSnapshot
            };
            Sessions[defender.Id].StartCombat(toDefender);
        }
    }

    private static CharacterSnapshot BuildPlayerSnapshot(IWarSession session, WarUnit unit)
    {
        var client = session.Client;
        CharacterSnapshot snapshot;
        lock (client.CharacterLock)
        {
            snapshot = new CharacterSnapshot(client.CharacterData ?? throw new InvalidOperationException("No character"));
        }

        snapshot.Hp = unit.Hp;
        snapshot.SinhMenh = unit.HpMax;
        return snapshot;
    }

    private static int? cuuUThuId;

    private static CharacterSnapshot BuildPuppetSnapshot(WarUnit unit)
    {
        var template = TcpServer.Instance.Scarecrow ?? throw new InvalidOperationException("Scarecrow chưa tải");
        var snapshot = new CharacterSnapshot(template);
        cuuUThuId ??= GameConfig.NDCPTTManager.ThanThongs.FirstOrDefault(x => x is MTS.TT.CuuUThu)?.Id;
        var puppet = unit.Puppet!;
        snapshot.Name = $"Khôi Lỗi #{puppet.Id}";
        snapshot.Level = puppet.Level;
        snapshot.Hp = puppet.Hp;
        snapshot.SinhMenh = puppet.HpMax;
        // Thọ nguyên vô tận, chỉ trang bị Cửu U Thứ.
        snapshot.ThoNguyen = int.MaxValue / 2;
        snapshot.TuoiTac = TimeSpan.Zero;
        snapshot.ThanThongEquip = cuuUThuId is { } id ? [id] : [];
        snapshot.VuKhi = null!;
        snapshot.PhongCu = null!;
        snapshot.TrangSuc = null!;
        snapshot.NgoDaos = [];
        snapshot.DanCombatUseds = [];
        snapshot.HasClan = Clans.TryGetValue(puppet.ClanId, out var clan);
        snapshot.ClanId = puppet.ClanId;
        snapshot.ClanName = clan?.Name ?? string.Empty;
        snapshot.ClanImageUrl = clan?.ImageUrl;
        snapshot.ClanLevel = clan?.Level ?? 0;
        snapshot.ClanInheritanceEffects = [];
        return snapshot;
    }

    // =====================================================================
    // Sự kiện từ màn hình combat
    // =====================================================================

    /// <summary>Client đã vào màn hình combat. Trả về false nếu trận không còn hợp lệ.</summary>
    public static bool OnCombatReady(long battleId, IWarCombatSide side)
    {
        IWarCombatSide[]? startPvp = null;
        int starter = 0;
        lock (Sync)
        {
            if (!Battles.TryGetValue(battleId, out var battle) || battle.Ended)
            {
                ResendEnded(battleId, side);
                return false;
            }

            var index = battle.SideOf(side.CharId);
            if (index < 0) return false;
            battle.Sides[index] = side;
            battle.Ready[index] = true;
            if (!battle.IsPvp)
            {
                battle.StartedAt ??= DateTime.UtcNow;
                return true;
            }

            if (battle.Ready[0] && battle.Ready[1] && battle.StartedAt == null)
            {
                battle.StartedAt = DateTime.UtcNow;
                startPvp = [battle.Sides[0]!, battle.Sides[1]!];
                starter = PickStarter(battle);
            }
        }

        if (startPvp != null) _ = StartPvpTurn(startPvp, starter);
        return true;
    }

    /// <summary>Bên đi trước là bên có Tốc độ bay cao hơn (giống PvP luận bàn).</summary>
    private static int PickStarter(WarBattleRuntime battle)
    {
        int Speed(WarUnit unit) =>
            Sessions.TryGetValue(unit.Id, out var s) && s.Client.CharacterData is { } c ? c.Stats.TocDoBay : 0;

        return Speed(battle.Attacker) >= Speed(battle.Defender) ? battle.Attacker.Id : battle.Defender.Id;
    }

    private static async Task StartPvpTurn(IWarCombatSide[] sides, int starter)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(2000));
        foreach (var side in sides)
        {
            side.MemSend(MessageTag.SyncStartTurn, starter);
            side.MemSend(MessageTag.Notify, "Trận đấu bắt đầu");
        }
    }

    /// <summary>Đối thủ PvP để chuyển tiếp các message Sync* trong trận.</summary>
    public static IWarCombatSide? GetPeer(long battleId, int charId)
    {
        lock (Sync)
        {
            if (!Battles.TryGetValue(battleId, out var battle) || battle.Ended || !battle.IsPvp) return null;
            var index = battle.SideOf(charId);
            if (index < 0 || battle.StartedAt == null) return null;
            return battle.Sides[1 - index];
        }
    }

    /// <summary>Client báo kết quả trận.</summary>
    public static void Report(long battleId, IWarCombatSide side, WarBattleReport report)
    {
        lock (Sync)
        {
            if (!Battles.TryGetValue(battleId, out var battle) || battle.Ended)
            {
                ResendEnded(battleId, side);
                return;
            }

            var index = battle.SideOf(side.CharId);
            if (index < 0) return;
            battle.Sides[index] ??= side;
            if (battle.Reports[index] != null) return;
            battle.Reports[index] = report;
            battle.FirstReportAt ??= DateTime.UtcNow;
            var now = DateTime.UtcNow;
            var type = report.Result?.TypeResultCombat ?? ETypeResultCombat.None;
            if (!battle.IsPvp)
            {
                ResolvePve(battle, report, type, now);
            }
            else if (type == ETypeResultCombat.LoseByRun)
            {
                // Bỏ chạy có hiệu lực ngay, không chờ báo cáo của bên kia.
                EndBattle(battle, index == 0 ? WarBattleOutcome.DefenderWin : WarBattleOutcome.AttackerWin,
                    WarBattleEndReason.Run, ReportedHp(battle, 0), ReportedHp(battle, 1), now, loserRan: true);
            }
            else if (battle.Reports[0] != null && battle.Reports[1] != null)
            {
                ResolvePvp(battle, now);
            }

            PublishDelta();
        }
    }

    /// <summary>Bên đang đánh mất kết nối/logout/rời trận: xử thua ngay (như bỏ chạy).</summary>
    public static void OnCombatSideLeft(long battleId, int charId)
    {
        lock (Sync)
        {
            if (!Battles.TryGetValue(battleId, out var battle) || battle.Ended) return;
            var index = battle.SideOf(charId);
            if (index < 0) return;
            var now = DateTime.UtcNow;
            EndBattle(battle, index == 0 ? WarBattleOutcome.DefenderWin : WarBattleOutcome.AttackerWin,
                WarBattleEndReason.Disconnect, ReportedHp(battle, 0), ReportedHp(battle, 1), now, loserRan: true);
            PublishDelta();
        }
    }

    private static int? ReportedHp(WarBattleRuntime battle, int side) =>
        battle.Reports[side]?.Result?.CurrentHPUser;

    private static void ResolvePve(WarBattleRuntime battle, WarBattleReport report, ETypeResultCombat type, DateTime now)
    {
        var playerHp = report.Result?.CurrentHPUser ?? battle.Attacker.Hp;
        var puppetHp = Math.Clamp(report.EnemyHp, 0, battle.Record.DefenderHpBefore);
        switch (type)
        {
            case ETypeResultCombat.Win:
                EndBattle(battle, WarBattleOutcome.AttackerWin, WarBattleEndReason.Died, playerHp, 0, now);
                break;
            case ETypeResultCombat.LoseByDied:
                EndBattle(battle, WarBattleOutcome.DefenderWin, WarBattleEndReason.Died, 0, Math.Max(1, puppetHp), now);
                break;
            case ETypeResultCombat.LoseByRun:
                EndBattle(battle, WarBattleOutcome.DefenderWin, WarBattleEndReason.Run, playerHp,
                    Math.Max(1, puppetHp), now, loserRan: true);
                break;
            default:
                EndBattle(battle, WarBattleOutcome.Draw, WarBattleEndReason.Draw, playerHp, puppetHp, now);
                break;
        }
    }

    /// <summary>Hai báo cáo khớp nhau thì áp dụng; mâu thuẫn thì hoà.</summary>
    private static void ResolvePvp(WarBattleRuntime battle, DateTime now)
    {
        var a = battle.Reports[0]!.Result?.TypeResultCombat ?? ETypeResultCombat.None;
        var b = battle.Reports[1]!.Result?.TypeResultCombat ?? ETypeResultCombat.None;
        var hpA = ReportedHp(battle, 0);
        var hpB = ReportedHp(battle, 1);
        if (a == ETypeResultCombat.Win && b == ETypeResultCombat.LoseByDied)
            EndBattle(battle, WarBattleOutcome.AttackerWin, WarBattleEndReason.Died, hpA, 0, now);
        else if (b == ETypeResultCombat.Win && a == ETypeResultCombat.LoseByDied)
            EndBattle(battle, WarBattleOutcome.DefenderWin, WarBattleEndReason.Died, 0, hpB, now);
        else
        {
            var reason = a == b && a is ETypeResultCombat.LoseByDied or ETypeResultCombat.LoseByTime
                ? WarBattleEndReason.Draw
                : WarBattleEndReason.Conflict;
            if (reason == WarBattleEndReason.Conflict)
                Log.Warning("TongMonChien battle {BattleId} conflicting reports {A}/{B} -> draw", battle.Id, a, b);
            EndBattle(battle, WarBattleOutcome.Draw, reason, hpA, hpB, now);
        }
    }

    private static void TickBattles(DateTime now)
    {
        foreach (var battle in Battles.Values.ToList())
        {
            if (battle.Ended) continue;
            if (battle.StartedAt == null && now - battle.CreatedAt > WarConfig.ReadyTimeout)
            {
                // Bên chưa vào trận bị xử thua; cả hai cùng chưa vào thì hoà.
                var aReady = battle.Ready[0];
                var dReady = battle.Ready[1] || !battle.IsPvp;
                var outcome = aReady && !dReady ? WarBattleOutcome.AttackerWin
                    : !aReady && dReady ? WarBattleOutcome.DefenderWin
                    : WarBattleOutcome.Draw;
                EndBattle(battle, outcome, WarBattleEndReason.Disconnect, null, null, now, loserRan: true);
                continue;
            }

            if (battle.IsPvp && battle.FirstReportAt is { } first && now - first > WarConfig.ReportTimeout)
            {
                // Chỉ một bên báo: bên còn lại coi như mất kết nối và thua.
                var missing = battle.Reports[0] == null ? 0 : 1;
                EndBattle(battle, missing == 0 ? WarBattleOutcome.DefenderWin : WarBattleOutcome.AttackerWin,
                    WarBattleEndReason.Disconnect, ReportedHp(battle, 0), ReportedHp(battle, 1), now, loserRan: true);
                continue;
            }

            if (now - battle.CreatedAt > WarConfig.BattleHardTimeout)
            {
                EndBattle(battle, WarBattleOutcome.Draw, WarBattleEndReason.Timeout, ReportedHp(battle, 0),
                    ReportedHp(battle, 1), now);
            }
        }
    }

    /// <summary>
    /// Kết thúc trận: áp HP (không vượt HP trước trận), đưa bên thua về thành chính (hết HP) hoặc node đồng minh
    /// gần nhất (bỏ chạy/mất kết nối), bên thắng/hoà ở lại node; sau đó quyết định lại node.
    /// </summary>
    private static void EndBattle(WarBattleRuntime battle, WarBattleOutcome outcome, WarBattleEndReason reason,
        int? attackerHp, int? defenderHp, DateTime now, bool loserRan = false)
    {
        if (battle.Ended) return;
        battle.Ended = true;
        Battles.Remove(battle.Id);
        var record = battle.Record;
        record.State = WarBattleState.Ended;
        record.Outcome = outcome;
        record.EndReason = reason;
        record.EndedAt = now;
        var hpBefore = new[] { record.AttackerHpBefore, record.DefenderHpBefore };
        var reported = new[] { attackerHp, defenderHp };
        for (var side = 0; side < 2; side++)
        {
            var unit = battle.UnitOfSide(side);
            var won = outcome == (side == 0 ? WarBattleOutcome.AttackerWin : WarBattleOutcome.DefenderWin);
            var lost = outcome == (side == 0 ? WarBattleOutcome.DefenderWin : WarBattleOutcome.AttackerWin);
            var hp = Math.Clamp(Math.Min(reported[side] ?? hpBefore[side], hpBefore[side]), 0, unit.HpMax);
            if (lost && !loserRan) hp = 0;
            if (won && hp < 1) hp = 1;
            if (side == 0) record.AttackerHpAfter = hp;
            else record.DefenderHpAfter = hp;
            if (!Units.ContainsKey(unit.Key)) continue;

            unit.BattleId = 0;
            unit.Hp = hp;
            unit.LastHpRecoveryAt = now;
            WarSideResult result;
            if (hp <= 0)
            {
                Relocate(unit, MainCityOf(unit.ClanId));
                result = lost ? WarSideResult.LoseDied : WarSideResult.Draw;
            }
            else if (lost)
            {
                Relocate(unit, unit.IsPuppet ? MainCityOf(unit.ClanId) : FindNearestAllyNode(unit.ClanId, battle.NodeId));
                result = WarSideResult.LoseRun;
            }
            else
            {
                unit.State = WarUnitState.Holding;
                result = won ? WarSideResult.Win : WarSideResult.Draw;
                if (!unit.IsPuppet) Returning[unit.Id] = now + ReturnGrace;
            }

            MarkUnit(unit);
            if (unit.IsPuppet) continue;
            var ended = new WarBattleEnded
            {
                BattleId = battle.Id,
                Result = result,
                Hp = unit.Hp,
                HpMax = unit.HpMax,
                NodeId = unit.NodeId,
                Message = result switch
                {
                    WarSideResult.Win => "Chiến thắng! Giữ nguyên HP còn lại.",
                    WarSideResult.LoseDied => "Thất bại, trở về thành chính.",
                    WarSideResult.LoseRun => "Rút lui về node đồng minh gần nhất.",
                    _ => "Bất phân thắng bại."
                }
            };
            CacheEnded(battle.Id, unit.Id, ended);
            try
            {
                battle.Sides[side]?.MemSend(MessageTag.WarBattleEnded, ended);
            }
            catch (Exception e)
            {
                Log.Warning(e, "TongMonChien send battle ended failed");
            }
        }

        endedBattlesToSave[battle.Id] = record;
        dirtyBattles.Add(battle.Id);
        Log.Information("TongMonChien battle {BattleId} ended node {NodeId}: {Outcome} ({Reason}) hp {AHp}/{DHp}",
            battle.Id, battle.NodeId, outcome, reason, record.AttackerHpAfter, record.DefenderHpAfter);
        if (Nodes.TryGetValue(battle.NodeId, out var node)) pendingNodes[node.NodeId] = node;
        ResolveNode(battle.NodeId, now);
    }

    private static void CacheEnded(long battleId, int charId, WarBattleEnded ended)
    {
        EndedResults[(battleId, charId)] = ended;
        EndedResultOrder.Enqueue((battleId, charId));
        while (EndedResultOrder.Count > 512) EndedResults.Remove(EndedResultOrder.Dequeue());
    }

    private static void ResendEnded(long battleId, IWarCombatSide side)
    {
        if (EndedResults.TryGetValue((battleId, side.CharId), out var ended))
            side.MemSend(MessageTag.WarBattleEnded, ended);
    }
}
