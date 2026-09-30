using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class AddTongMonChien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NodeId",
                table: "ClanMembers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClanNodes",
                columns: table => new
                {
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    ClanId = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanNodes", x => x.NodeId);
                    table.ForeignKey(
                        name: "FK_ClanNodes_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Puppets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    RandomSeed = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    Hp = table.Column<int>(type: "int", nullable: false),
                    HpMax = table.Column<int>(type: "int", nullable: false),
                    LastHpRecoveryAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    State = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Route = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NextArrivalAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    AppearanceJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puppets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Puppets_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarBattles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    IsPvp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttackerCharId = table.Column<int>(type: "int", nullable: false),
                    AttackerClanId = table.Column<int>(type: "int", nullable: false),
                    DefenderCharId = table.Column<int>(type: "int", nullable: true),
                    DefenderPuppetId = table.Column<int>(type: "int", nullable: true),
                    DefenderClanId = table.Column<int>(type: "int", nullable: false),
                    AttackerHpBefore = table.Column<int>(type: "int", nullable: false),
                    DefenderHpBefore = table.Column<int>(type: "int", nullable: false),
                    AttackerHpAfter = table.Column<int>(type: "int", nullable: false),
                    DefenderHpAfter = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Outcome = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    EndReason = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarBattles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarParticipants",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    Hp = table.Column<int>(type: "int", nullable: false),
                    HpMax = table.Column<int>(type: "int", nullable: false),
                    LastHpRecoveryAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    State = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Route = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NextArrivalAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastSafeNodeId = table.Column<int>(type: "int", nullable: false),
                    ArrivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastEnterAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarParticipants", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_WarParticipants_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarWeeklySettlements",
                columns: table => new
                {
                    WeekStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClanCount = table.Column<int>(type: "int", nullable: false),
                    MailCount = table.Column<int>(type: "int", nullable: false),
                    TotalMoneyVip = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarWeeklySettlements", x => x.WeekStart);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ClanNodes_ClanId",
                table: "ClanNodes",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_Puppets_ClanId",
                table: "Puppets",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_WarBattles_NodeId",
                table: "WarBattles",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarBattles_State",
                table: "WarBattles",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_WarParticipants_ClanId",
                table: "WarParticipants",
                column: "ClanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClanNodes");

            migrationBuilder.DropTable(
                name: "Puppets");

            migrationBuilder.DropTable(
                name: "WarBattles");

            migrationBuilder.DropTable(
                name: "WarParticipants");

            migrationBuilder.DropTable(
                name: "WarWeeklySettlements");

            migrationBuilder.DropColumn(
                name: "NodeId",
                table: "ClanMembers");
        }
    }
}
