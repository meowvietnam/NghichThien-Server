using Microsoft.EntityFrameworkCore;
using MTS.Models;

namespace MTS.Data;

public partial class AppDbContext
{
    public DbSet<Clan> Clans => Set<Clan>();
    public DbSet<ClanMember> ClanMembers => Set<ClanMember>();
    public DbSet<ClanMembershipState> ClanMembershipStates => Set<ClanMembershipState>();
    public DbSet<ClanApplication> ClanApplications => Set<ClanApplication>();
    public DbSet<ClanLedgerEntry> ClanLedgerEntries => Set<ClanLedgerEntry>();
    public DbSet<ClanMemberPeriodStats> ClanMemberPeriodStats => Set<ClanMemberPeriodStats>();
    public DbSet<ClanRewardClaim> ClanRewardClaims => Set<ClanRewardClaim>();
    public DbSet<ClanAuditLog> ClanAuditLogs => Set<ClanAuditLog>();
    public DbSet<ClanCommandReceipt> ClanCommandReceipts => Set<ClanCommandReceipt>();
    public DbSet<ClanOutboxEvent> ClanOutboxEvents => Set<ClanOutboxEvent>();

    public DbSet<ClanCreationPayment> ClanCreationPayments => Set<ClanCreationPayment>();
    public DbSet<ClanInheritance> ClanInheritances => Set<ClanInheritance>();
    public DbSet<ClanMemberInheritance> ClanMemberInheritances => Set<ClanMemberInheritance>();

    public static void ConfigureClans(ModelBuilder b)
    {
        b.Entity<ClanCreationPayment>(e =>
        {
            e.HasKey(x => x.OrderId);
            e.Property(x => x.OrderId).ValueGeneratedNever();
            e.HasIndex(x => new { x.CharId, x.RequestId }).IsUnique();
            e.HasIndex(x => new { x.CharId, x.Status });
            e.Property(x => x.CheckoutUrl).HasMaxLength(1024);
            e.Property(x => x.PaymentLinkId).HasMaxLength(128);
            e.Property(x => x.PaymentReference).HasMaxLength(128);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.CharId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.ConsumedByClanId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanInheritance>(e =>
        {
            e.HasKey(x => new { x.ClanId, x.InheritanceId });
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.ClanId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanMemberInheritance>(e =>
        {
            e.HasKey(x => new { x.CharId, x.ClanId, x.InheritanceId });
            e.HasOne<ClanMember>().WithMany().HasForeignKey(x => x.CharId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ClanInheritance>().WithMany().HasForeignKey(x => new { x.ClanId, x.InheritanceId }).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Clan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(64);
            e.Property(x => x.NormalizedName).HasMaxLength(64).UseCollation("utf8mb4_bin");
            e.HasIndex(x => x.NormalizedName).IsUnique();
            e.Property(x => x.ImageUrl).HasMaxLength(512);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.Announcement).HasMaxLength(1024);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.LeaderCharId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("Clans", t => {
                t.HasCheckConstraint("CK_Clan_Funds", "`Funds` >= 0 AND `JadeFunds` >= 0");
                t.HasCheckConstraint("CK_Clan_Members", "`MemberCount` >= 0 AND `MemberCount` <= `MaxMembers`");
            });
        });
        b.Entity<ClanMember>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasIndex(x => new { x.ClanId, x.CharId });
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.CharId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.ClanId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("ClanMembers", t => {
                t.HasCheckConstraint("CK_ClanMember_Contribution", "`TotalContribution` >= 0 AND `AvailableContribution` >= 0");
                t.HasCheckConstraint("CK_ClanMember_Role", "`Role` >= 0 AND `Role` <= 2");
            });
        });
        b.Entity<ClanMembershipState>(e =>
        {
            e.HasKey(x => x.CharId);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.CharId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.LastClanId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanApplication>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ClanId, x.CharId }).IsUnique();
            e.HasIndex(x => new { x.ClanId, x.Status, x.CreatedAt, x.Id });
            e.HasIndex(x => new { x.CharId, x.Status });
            e.Property(x => x.Message).HasMaxLength(256);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.ClanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.CharId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.ReviewedByCharId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanLedgerEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ClanId, x.Id });
            e.HasIndex(x => x.TransactionId);
            e.Property(x => x.Reason).HasMaxLength(64);
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.ClanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.ActorCharId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanMemberPeriodStats>(e =>
        {
            e.HasKey(x => new { x.CharId, x.ScopeId, x.PeriodId, x.CounterType });
            e.Property(x => x.CounterType).HasMaxLength(32);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.CharId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanRewardClaim>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RewardKey).HasMaxLength(128);
            e.HasIndex(x => new { x.CharId, x.RewardKey }).IsUnique();
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.CharId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.SourceClanId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanAuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ClanId, x.Id });
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.Data).HasColumnType("json");
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.ClanId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanCommandReceipt>(e =>
        {
            e.HasKey(x => new { x.ActorCharId, x.RequestId });
            e.Property(x => x.CommandType).HasMaxLength(64);
            e.Property(x => x.PayloadHash).HasMaxLength(64);
            e.Property(x => x.ResultPayload).HasColumnType("json");
            e.HasOne<CharacterData>().WithMany().HasForeignKey(x => x.ActorCharId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ClanOutboxEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => new { x.PublishedAt, x.NextAttemptAt, x.Id });
            e.Property(x => x.EventType).HasMaxLength(64);
            e.HasOne<Clan>().WithMany().HasForeignKey(x => x.ClanId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
