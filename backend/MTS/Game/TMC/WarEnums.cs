namespace MTS.Game.TMC;

public enum WarUnitState : byte
{
    /// <summary>Đứng yên tại node, sẵn sàng phòng thủ/ghép trận.</summary>
    Idle,
    /// <summary>Đang đi trên một cạnh, tới node kế tiếp lúc NextArrivalAt.</summary>
    Moving,
    /// <summary>Đang đánh một trận.</summary>
    Fighting,
    /// <summary>Đã tới node đang giao tranh, chờ ghép trận hoặc chờ node được giải quyết để đi tiếp.</summary>
    Holding
}

public enum WarUnitKind : byte
{
    Player,
    Puppet
}

public enum WarBattleState : byte
{
    Running,
    Ended,
    Cancelled
}

public enum WarBattleOutcome : byte
{
    None,
    AttackerWin,
    DefenderWin,
    Draw
}

public enum WarBattleEndReason : byte
{
    None,
    Died,
    Run,
    Disconnect,
    Timeout,
    Conflict,
    Draw,
    ClanRemoved,
    ServerRestart,
    SeasonReset,
    WarClosed
}

/// <summary>Kết quả trận nhìn từ phía một người chơi.</summary>
public enum WarSideResult : byte
{
    Win,
    LoseDied,
    LoseRun,
    Draw
}

/// <summary>Cảnh giới rối, theo thứ tự Luyện Khí → Hóa Thần.</summary>
public enum WarRealm : byte
{
    LuyenKhi,
    TrucCo,
    KimDan,
    NguyenAnh,
    HoaThan
}
