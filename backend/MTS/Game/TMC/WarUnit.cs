using MTS.Models;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.TMC;

/// <summary>Phiên người chơi đang mở scene Tông môn chiến (được tính là đối thủ tại node).</summary>
public interface IWarSession
{
    int CharId { get; }
    Client Client { get; }
    void MemSend<T>(MessageTag tag, T data);
    /// <summary>Server buộc người chơi rời chiến (tông giải tán, bị kick...).</summary>
    void Kick(string message);
    /// <summary>Server đã ghép trận: gửi thông tin trận và chuyển client sang state combat.</summary>
    void StartCombat(WarBattleMatched matched);
}

/// <summary>Một bên của trận đang ở màn hình combat.</summary>
public interface IWarCombatSide
{
    int CharId { get; }
    void MemSend<T>(MessageTag tag, T data);
}

/// <summary>Lớp bọc chung cho người chơi và rối để xử lý di chuyển/ghép trận thống nhất.</summary>
internal sealed class WarUnit
{
    public WarUnitKind Kind { get; }
    public WarParticipant? Player { get; }
    public Puppet? Puppet { get; }
    public List<int> Route { get; private set; }
    /// <summary>Trận đang tham gia (khi State = Fighting).</summary>
    public long BattleId { get; set; }

    // ---- Trạng thái trên cạnh đang đi (chỉ trong bộ nhớ; restart thì đối tượng đang đi dừng ở node xuất phát) ----

    /// <summary>Thời gian đi hết cạnh hiện tại (giây), chốt lúc bắt đầu cạnh theo độn tốc khi đó.</summary>
    public double EdgeSeconds { get; set; }
    /// <summary>Đang đánh giữa đường: phần thời gian còn lại của cạnh lúc bị dừng. Null khi không đánh giữa đường.</summary>
    public TimeSpan? EdgeRemaining { get; set; }
    /// <summary>Vừa thắng/hoà trận giữa đường và đang đi nốt cạnh: vào lại scene không bị huỷ lộ trình.</summary>
    public bool EdgeResume { get; set; }
    /// <summary>Đối thủ đã gặp trên cạnh hiện tại (không ghép lại lần nữa trên cùng cạnh).</summary>
    public HashSet<(WarUnitKind, int)> EdgePassed { get; } = new();

    /// <summary>Tiến độ trên cạnh hiện tại (0 = node xuất phát, 1 = node kế) tại thời điểm <paramref name="at"/>.</summary>
    public double EdgeProgressAt(DateTime at)
    {
        if (EdgeSeconds <= 0) return 0;
        var remaining = EdgeRemaining ?? (NextArrivalAt is { } arrival ? arrival - at : TimeSpan.Zero);
        return Math.Clamp(1 - remaining.TotalSeconds / EdgeSeconds, 0, 1);
    }

    public void ClearEdge()
    {
        EdgeRemaining = null;
        EdgeResume = false;
        EdgePassed.Clear();
    }

    public WarUnit(WarParticipant player)
    {
        Kind = WarUnitKind.Player;
        Player = player;
        Route = ParseRoute(player.Route);
    }

    public WarUnit(Puppet puppet)
    {
        Kind = WarUnitKind.Puppet;
        Puppet = puppet;
        Route = ParseRoute(puppet.Route);
    }

    public (WarUnitKind, int) Key => (Kind, Id);
    public int Id => Kind == WarUnitKind.Player ? Player!.CharId : Puppet!.Id;
    public bool IsPuppet => Kind == WarUnitKind.Puppet;
    /// <summary>Rối trung lập canh thành trung lập: không thuộc tông nào, không lưu DB, không di chuyển.</summary>
    public bool IsNeutral => Kind == WarUnitKind.Puppet && Puppet!.ClanId == WarConfig.NeutralClanId;

    public int ClanId
    {
        get => Kind == WarUnitKind.Player ? Player!.ClanId : Puppet!.ClanId;
        set { if (Player != null) Player.ClanId = value; else Puppet!.ClanId = value; }
    }

    public int NodeId
    {
        get => Kind == WarUnitKind.Player ? Player!.NodeId : Puppet!.NodeId;
        set { if (Player != null) Player.NodeId = value; else Puppet!.NodeId = value; }
    }

    public int Hp
    {
        get => Kind == WarUnitKind.Player ? Player!.Hp : Puppet!.Hp;
        set { if (Player != null) Player.Hp = value; else Puppet!.Hp = value; }
    }

    public int HpMax
    {
        get => Kind == WarUnitKind.Player ? Player!.HpMax : Puppet!.HpMax;
        set { if (Player != null) Player.HpMax = value; else Puppet!.HpMax = value; }
    }

    public DateTime LastHpRecoveryAt
    {
        get => Kind == WarUnitKind.Player ? Player!.LastHpRecoveryAt : Puppet!.LastHpRecoveryAt;
        set { if (Player != null) Player.LastHpRecoveryAt = value; else Puppet!.LastHpRecoveryAt = value; }
    }

    public WarUnitState State
    {
        get => Kind == WarUnitKind.Player ? Player!.State : Puppet!.State;
        set { if (Player != null) Player.State = value; else Puppet!.State = value; }
    }

    public DateTime? NextArrivalAt
    {
        get => Kind == WarUnitKind.Player ? Player!.NextArrivalAt : Puppet!.NextArrivalAt;
        set { if (Player != null) Player.NextArrivalAt = value; else Puppet!.NextArrivalAt = value; }
    }

    public DateTime ArrivedAt
    {
        get => Kind == WarUnitKind.Player ? Player!.ArrivedAt : Puppet!.ArrivedAt;
        set { if (Player != null) Player.ArrivedAt = value; else Puppet!.ArrivedAt = value; }
    }

    public float RegenPerMinute => Kind == WarUnitKind.Player
        ? WarConfig.GetPlayerRegenPerMinute(HpMax)
        : WarConfig.GetPuppetRegenPerMinute(HpMax);

    public void SetRoute(IEnumerable<int> route)
    {
        Route = route.ToList();
        var text = string.Join(',', Route);
        if (Player != null) Player.Route = text;
        else Puppet!.Route = text;
    }

    public void ClearRoute()
    {
        SetRoute(Array.Empty<int>());
        NextArrivalAt = null;
        ClearEdge();
    }

    /// <summary>Áp phần HP hồi theo thời gian server kể từ mốc hồi gần nhất (tính lười, gồm cả lúc offline).</summary>
    public bool ApplyRegen(DateTime now)
    {
        if (State == WarUnitState.Fighting) return false;
        if (Hp >= HpMax)
        {
            LastHpRecoveryAt = now;
            return false;
        }

        var regen = RegenPerMinute;
        if (regen <= 0) return false;
        var elapsedMinutes = (now - LastHpRecoveryAt).TotalMinutes;
        if (elapsedMinutes <= 0) return false;
        var gained = (int)Math.Floor(elapsedMinutes * regen);
        if (gained <= 0) return false;
        Hp = Math.Min(HpMax, Hp + gained);
        LastHpRecoveryAt = Hp >= HpMax ? now : LastHpRecoveryAt.AddMinutes(gained / regen);
        return true;
    }

    private static List<int> ParseRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route)) return new List<int>();
        return route.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
    }
}

internal sealed class WarNodeState
{
    public int NodeId { get; init; }
    public int? OwnerClanId { get; set; }
    public long Version { get; set; }
    /// <summary>Có trận đã diễn ra kể từ lần quyết định owner gần nhất (dùng cho luật "không còn ai → trung lập").</summary>
    public bool Contested { get; set; }
}

internal sealed class WarClanInfo
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int MainNodeId { get; set; }
    public int Level { get; set; }
    public int LeaderCharId { get; set; }
}

internal sealed class WarBattleRuntime
{
    public required WarBattle Record { get; init; }
    public long Id => Record.Id;
    /// <summary>Node diễn ra trận; 0 với trận giữa đường (Record.NodeId khi đó chỉ ghi node xuất phát của bên tấn công).</summary>
    public int NodeId => IsEdge ? 0 : Record.NodeId;
    public bool IsPvp => Record.IsPvp;
    public required WarUnit Attacker { get; init; }
    public required WarUnit Defender { get; init; }
    /// <summary>Index 0 = attacker, 1 = defender (chỉ có khi PvP).</summary>
    public IWarCombatSide?[] Sides { get; } = new IWarCombatSide?[2];
    public bool[] Ready { get; } = new bool[2];
    public WarBattleReport?[] Reports { get; } = new WarBattleReport?[2];
    public DateTime? FirstReportAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime CreatedAt => Record.CreatedAt;
    public bool Ended { get; set; }
    /// <summary>Trận ghép giữa đường (hai đơn vị gặp nhau trên một cạnh): không gắn với node nào.</summary>
    public bool IsEdge { get; init; }

    public int SideOf(int charId)
    {
        if (Attacker.Kind == WarUnitKind.Player && Attacker.Id == charId) return 0;
        if (Defender.Kind == WarUnitKind.Player && Defender.Id == charId) return 1;
        return -1;
    }

    public WarUnit UnitOfSide(int side) => side == 0 ? Attacker : Defender;
}
