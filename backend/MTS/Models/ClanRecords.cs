namespace MTS.Models;

public enum ClanJoinPolicy : byte
{
    Open,
    Approval,
    Closed
}

public enum ClanStatus : byte
{
    Active,
    Dissolved
}

public enum ClanApplicationStatus : byte
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Expired
}

public enum ClanLeaveReason : byte
{
    Leave,
    Kick,
    Dissolved
}

public enum ClanLedgerAccount : byte
{
    Funds,
    Contribution,
    CharacterLinhThach,
    CharacterMoneyVip,
    JadeFunds
}

public class ClanMembershipState
{
    public int CharId { get; set; }
    public int? LastClanId { get; set; }
    public DateTime? LeftAt { get; set; }
    public DateTime? NextJoinAt { get; set; }
    public ClanLeaveReason? LeaveReason { get; set; }
    public long Version { get; set; } = 1;
}

public class ClanApplication
{
    public long Id { get; set; }
    public int ClanId { get; set; }
    public int CharId { get; set; }
    public string Message { get; set; } = "";
    public ClanApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? ReviewedByCharId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long Version { get; set; } = 1;
}

public class ClanLedgerEntry
{
    public long Id { get; set; }
    public Guid TransactionId { get; set; }
    public int ClanId { get; set; }
    public int ActorCharId { get; set; }
    public ClanLedgerAccount AccountType { get; set; }
    public long Delta { get; set; }
    public long BalanceAfter { get; set; }
    public string Reason { get; set; } = "";
    public Guid RequestId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClanMemberPeriodStats
{
    public int CharId { get; set; }
    public int ScopeId { get; set; } // 0 = character-wide, survives changing clans.
    public int PeriodId { get; set; } // yyyyMMdd in configured server calendar.
    public string CounterType { get; set; } = "";
    public long Value { get; set; }
    public long Version { get; set; } = 1;
}

public class ClanRewardClaim
{
    public long Id { get; set; }
    public int CharId { get; set; }
    public string RewardKey { get; set; } = "";
    public int SourceClanId { get; set; }
    public Guid RequestId { get; set; }
    public DateTime ClaimedAt { get; set; }
}

public class ClanAuditLog
{
    public long Id { get; set; }
    public int ClanId { get; set; }
    public int ActorCharId { get; set; }
    public int? TargetCharId { get; set; }
    public string Action { get; set; } = "";
    public string Data { get; set; } = "{}";
    public Guid RequestId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClanCommandReceipt
{
    public int ActorCharId { get; set; }
    public Guid RequestId { get; set; }
    public string CommandType { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public string ResultPayload { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}

public class ClanOutboxEvent
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public int ClanId { get; set; }
    public int? TargetCharId { get; set; }
    public long ClanVersion { get; set; }
    public string EventType { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime NextAttemptAt { get; set; }
}
