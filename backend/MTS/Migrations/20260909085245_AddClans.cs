using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class AddClans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClanCommandReceipts",
                columns: table => new
                {
                    ActorCharId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CommandType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultPayload = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanCommandReceipts", x => new { x.ActorCharId, x.RequestId });
                    table.ForeignKey(
                        name: "FK_ClanCommandReceipts_Characters_ActorCharId",
                        column: x => x.ActorCharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanMemberPeriodStats",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ScopeId = table.Column<int>(type: "int", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: false),
                    CounterType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanMemberPeriodStats", x => new { x.CharId, x.ScopeId, x.PeriodId, x.CounterType });
                    table.ForeignKey(
                        name: "FK_ClanMemberPeriodStats_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImageUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmblemId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Announcement = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LeaderCharId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    Funds = table.Column<long>(type: "bigint", nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: false),
                    MaxMembers = table.Column<int>(type: "int", nullable: false),
                    JoinPolicy = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    MinLevel = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DissolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clans", x => x.Id);
                    table.CheckConstraint("CK_Clan_Funds", "`Funds` >= 0");
                    table.CheckConstraint("CK_Clan_Members", "`MemberCount` >= 0 AND `MemberCount` <= `MaxMembers`");
                    table.ForeignKey(
                        name: "FK_Clans_Characters_LeaderCharId",
                        column: x => x.LeaderCharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanApplications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewedByCharId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanApplications_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanApplications_Characters_ReviewedByCharId",
                        column: x => x.ReviewedByCharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanApplications_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    ActorCharId = table.Column<int>(type: "int", nullable: false),
                    TargetCharId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanAuditLogs_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TransactionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    ActorCharId = table.Column<int>(type: "int", nullable: false),
                    AccountType = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Delta = table.Column<long>(type: "bigint", nullable: false),
                    BalanceAfter = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanLedgerEntries_Characters_ActorCharId",
                        column: x => x.ActorCharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanLedgerEntries_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanMembers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    TotalContribution = table.Column<long>(type: "bigint", nullable: false),
                    AvailableContribution = table.Column<long>(type: "bigint", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanMembers", x => x.CharId);
                    table.CheckConstraint("CK_ClanMember_Contribution", "`TotalContribution` >= 0 AND `AvailableContribution` >= 0");
                    table.CheckConstraint("CK_ClanMember_Role", "`Role` >= 0 AND `Role` <= 2");
                    table.ForeignKey(
                        name: "FK_ClanMembers_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanMembers_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanMembershipStates",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    LastClanId = table.Column<int>(type: "int", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NextJoinAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LeaveReason = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanMembershipStates", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_ClanMembershipStates_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanMembershipStates_Clans_LastClanId",
                        column: x => x.LastClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanOutboxEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    TargetCharId = table.Column<int>(type: "int", nullable: true),
                    ClanVersion = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanOutboxEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanOutboxEvents_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanRewardClaims",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    RewardKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceClanId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClaimedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanRewardClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanRewardClaims_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanRewardClaims_Clans_SourceClanId",
                        column: x => x.SourceClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ClanApplications_CharId_Status",
                table: "ClanApplications",
                columns: new[] { "CharId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClanApplications_ClanId_CharId",
                table: "ClanApplications",
                columns: new[] { "ClanId", "CharId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClanApplications_ClanId_Status_CreatedAt_Id",
                table: "ClanApplications",
                columns: new[] { "ClanId", "Status", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ClanApplications_ReviewedByCharId",
                table: "ClanApplications",
                column: "ReviewedByCharId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanAuditLogs_ClanId_Id",
                table: "ClanAuditLogs",
                columns: new[] { "ClanId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ClanLedgerEntries_ActorCharId",
                table: "ClanLedgerEntries",
                column: "ActorCharId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanLedgerEntries_ClanId_Id",
                table: "ClanLedgerEntries",
                columns: new[] { "ClanId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ClanLedgerEntries_TransactionId",
                table: "ClanLedgerEntries",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanMembers_ClanId_CharId",
                table: "ClanMembers",
                columns: new[] { "ClanId", "CharId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClanMembershipStates_LastClanId",
                table: "ClanMembershipStates",
                column: "LastClanId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanOutboxEvents_ClanId",
                table: "ClanOutboxEvents",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanOutboxEvents_EventId",
                table: "ClanOutboxEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClanOutboxEvents_PublishedAt_NextAttemptAt_Id",
                table: "ClanOutboxEvents",
                columns: new[] { "PublishedAt", "NextAttemptAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ClanRewardClaims_CharId_RewardKey",
                table: "ClanRewardClaims",
                columns: new[] { "CharId", "RewardKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClanRewardClaims_SourceClanId",
                table: "ClanRewardClaims",
                column: "SourceClanId");

            migrationBuilder.CreateIndex(
                name: "IX_Clans_LeaderCharId",
                table: "Clans",
                column: "LeaderCharId");

            migrationBuilder.CreateIndex(
                name: "IX_Clans_NormalizedName",
                table: "Clans",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClanApplications");

            migrationBuilder.DropTable(
                name: "ClanAuditLogs");

            migrationBuilder.DropTable(
                name: "ClanCommandReceipts");

            migrationBuilder.DropTable(
                name: "ClanLedgerEntries");

            migrationBuilder.DropTable(
                name: "ClanMemberPeriodStats");

            migrationBuilder.DropTable(
                name: "ClanMembers");

            migrationBuilder.DropTable(
                name: "ClanMembershipStates");

            migrationBuilder.DropTable(
                name: "ClanOutboxEvents");

            migrationBuilder.DropTable(
                name: "ClanRewardClaims");

            migrationBuilder.DropTable(
                name: "Clans");
        }
    }
}
