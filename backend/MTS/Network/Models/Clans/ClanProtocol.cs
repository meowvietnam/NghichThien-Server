#nullable disable
using System;

// Keep byte-for-byte identical to the Unity copy. JSON wire contract v1.
namespace Network.Models.Clans
{
    public enum ClanCommandType
    {
        Create, Apply, CancelApplication, ReviewApplication, Leave, Kick,
        ChangeRole, TransferLeadership, UpdateSettings, Donate, Dissolve
    }
    public enum ClanQueryType { Search, Detail, Members, Applications, MyApplications, Audit, Ledger, Mine }
    public class ClanCommandRequest
    {
        public Guid RequestId { get; set; }
        public ClanCommandType Command { get; set; }
        public int ClanId { get; set; }
        public int TargetCharId { get; set; }
        public long ExpectedVersion { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Announcement { get; set; } = "";
        public string Message { get; set; } = "";
        public int EmblemId { get; set; }
        public int JoinPolicy { get; set; } = 1;
        public int MinLevel { get; set; }
        public int Role { get; set; }
        public int Amount { get; set; }
        public bool Approve { get; set; }
    }
    public class ClanQueryRequest
    {
        public Guid RequestId { get; set; }
        public ClanQueryType Query { get; set; }
        public int ClanId { get; set; }
        public string Search { get; set; } = "";
        public long AfterId { get; set; }
        public int Limit { get; set; } = 20;
    }
    public class ClanSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int EmblemId { get; set; }
        public int Level { get; set; }
        public int MemberCount { get; set; }
        public int MaxMembers { get; set; }
        public int JoinPolicy { get; set; }
        public int MinLevel { get; set; }
    }
    public class ClanDetailDto : ClanSummaryDto
    {
        public string Description { get; set; } = "";
        public string Announcement { get; set; } = "";
        public int LeaderCharId { get; set; }
        public long Funds { get; set; }
        public long Version { get; set; }
        public int MyRole { get; set; } = -1;
        public long MyContribution { get; set; }
        public long MyAvailableContribution { get; set; }
    }
    public class ClanMemberDto
    {
        public int CharId { get; set; }
        public string Name { get; set; } = "";
        public int Role { get; set; }
        public long Contribution { get; set; }
        public DateTime JoinedAt { get; set; }
    }
    public class ClanApplicationDto
    {
        public long Id { get; set; }
        public int ClanId { get; set; }
        public int CharId { get; set; }
        public string Message { get; set; } = "";
        public int Status { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
    public class ClanLogDto
    {
        public long Id { get; set; }
        public int ActorCharId { get; set; }
        public int? TargetCharId { get; set; }
        public string Action { get; set; } = "";
        public long Delta { get; set; }
        public long BalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class ClanResponse
    {
        public Guid RequestId { get; set; }
        public bool Success { get; set; }
        public string Code { get; set; } = "";
        public int ClanId { get; set; }
        public long Version { get; set; }
        public int? LinhThach { get; set; }
        public long NextCursor { get; set; }
        public ClanDetailDto Detail { get; set; }
        public ClanSummaryDto[] Clans { get; set; }
        public ClanMemberDto[] Members { get; set; }
        public ClanApplicationDto[] Applications { get; set; }
        public ClanLogDto[] Logs { get; set; }
    }
    // Invalidation only: no private data. Fetch an authorized snapshot after this event.
    public class ClanChangedDto
    {
        public Guid EventId { get; set; }
        public int ClanId { get; set; }
        public long Version { get; set; }
    }
}
