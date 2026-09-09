#nullable disable
using System;
using MemoryPack;
#if UNITY_5_3_OR_NEWER
using Base;
using CreateCharacter;
#else
using MTS.Models;
#endif

// Keep byte-for-byte identical to the Unity copy. MemoryPack wire contract v3. Preserve member declaration order.
namespace Network.Models.Clans
{
    public enum ClanCommandType
    {
        Create, Apply, CancelApplication, ReviewApplication, Leave, Kick,
        ChangeRole, TransferLeadership, UpdateSettings, Donate, Dissolve,
        UnlockInheritance, LearnInheritance, SelectInheritance
    }
    public enum ClanQueryType { Search, Detail, Members, Applications, MyApplications, Audit, Ledger, Mine, Inheritances, Payments }
    [MemoryPackable]
    public partial class ClanCommandRequest
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
        public bool FreeDonation { get; set; }
        public int DonationCount { get; set; } = 1;
        public int CastleNodeId { get; set; }
        public int CreationPaymentMethod { get; set; }
        public long PaymentOrderId { get; set; }
        public int InheritanceId { get; set; }
        public bool Approve { get; set; }
    }
    [MemoryPackable]
    public partial class ClanQueryRequest
    {
        public Guid RequestId { get; set; }
        public ClanQueryType Query { get; set; }
        public int ClanId { get; set; }
        public string Search { get; set; } = "";
        public long AfterId { get; set; }
        public int Limit { get; set; } = 20;
    }
    [MemoryPackable]
    public partial class ClanSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int EmblemId { get; set; }
        public int Level { get; set; }
        public int MemberCount { get; set; }
        public int MaxMembers { get; set; }
        public int JoinPolicy { get; set; }
        public int MinLevel { get; set; }
        public string ImageUrl { get; set; }
    }
    [MemoryPackable]
    public partial class ClanDetailDto : ClanSummaryDto
    {
        public string Description { get; set; } = "";
        public string Announcement { get; set; } = "";
        public int LeaderCharId { get; set; }
        public long Funds { get; set; }
        public long JadeFunds { get; set; }
        public int? CastleNodeId { get; set; }
        public bool FreeDonationAvailable { get; set; }
        public int DonationUnitMoneyVip { get; set; }
        public int? ActiveInheritanceId { get; set; }
        public long Version { get; set; }
        public int MyRole { get; set; } = -1;
        public long MyContribution { get; set; }
        public long MyAvailableContribution { get; set; }
    }
    [MemoryPackable]
    public partial class ClanMemberDto
    {
        public int CharId { get; set; }
        public string Name { get; set; } = "";
        public int Role { get; set; }
        public long Contribution { get; set; }
        public DateTime JoinedAt { get; set; }
        public Gender Gender { get; set; }
        public Appearance Appearance { get; set; } = new Appearance();
    }
    [MemoryPackable]
    public partial class ClanApplicationDto
    {
        public long Id { get; set; }
        public int ClanId { get; set; }
        public int CharId { get; set; }
        public string Message { get; set; } = "";
        public int Status { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
    [MemoryPackable]
    public partial class ClanLogDto
    {
        public long Id { get; set; }
        public int ActorCharId { get; set; }
        public int? TargetCharId { get; set; }
        public string Action { get; set; } = "";
        public long Delta { get; set; }
        public long BalanceAfter { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    [MemoryPackable]
    public partial class ClanResponse
    {
        public Guid RequestId { get; set; }
        public bool Success { get; set; }
        public string Code { get; set; } = "";
        public int ClanId { get; set; }
        public long Version { get; set; }
        public int? LinhThach { get; set; }
        public int? MoneyVip { get; set; }
        public ClanPaymentDto[] Payments { get; set; }
        public ClanInheritanceDto[] Inheritances { get; set; }
        public long NextCursor { get; set; }
        public ClanDetailDto Detail { get; set; }
        public ClanSummaryDto[] Clans { get; set; }
        public ClanMemberDto[] Members { get; set; }
        public ClanApplicationDto[] Applications { get; set; }
        public ClanLogDto[] Logs { get; set; }
    }
    [MemoryPackable]
    public partial class ClanPaymentDto
    {
        public long OrderId { get; set; }
        public long Amount { get; set; }
        public string CheckoutUrl { get; set; }
        public int Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int? ClanId { get; set; }
    }
    [MemoryPackable]
    public partial class ClanInheritanceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Effect { get; set; }
        public int Element { get; set; }
        public int Value { get; set; }
        public long UnlockCost { get; set; }
        public long LearnCost { get; set; }
        public int MinClanLevel { get; set; }
        public bool Unlocked { get; set; }
        public bool Learned { get; set; }
    }
    [MemoryPackable]
    public partial class ClanChangedDto
    {
        public Guid EventId { get; set; }
        public int ClanId { get; set; }
        public long Version { get; set; }
    }
}
