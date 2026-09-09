using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class ClanEconomyCastleInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CastleNodeId",
                table: "Clans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "JadeFunds",
                table: "Clans",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ActiveInheritanceId",
                table: "ClanMembers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClanCreationPayments",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    CheckoutUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentLinkId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentReference = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ConsumedByClanId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanCreationPayments", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_ClanCreationPayments_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanCreationPayments_Clans_ConsumedByClanId",
                        column: x => x.ConsumedByClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanInheritances",
                columns: table => new
                {
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    InheritanceId = table.Column<int>(type: "int", nullable: false),
                    UnlockedByCharId = table.Column<int>(type: "int", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanInheritances", x => new { x.ClanId, x.InheritanceId });
                    table.ForeignKey(
                        name: "FK_ClanInheritances_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClanMemberInheritances",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    InheritanceId = table.Column<int>(type: "int", nullable: false),
                    LearnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanMemberInheritances", x => new { x.CharId, x.ClanId, x.InheritanceId });
                    table.ForeignKey(
                        name: "FK_ClanMemberInheritances_ClanInheritances_ClanId_InheritanceId",
                        columns: x => new { x.ClanId, x.InheritanceId },
                        principalTable: "ClanInheritances",
                        principalColumns: new[] { "ClanId", "InheritanceId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClanMemberInheritances_ClanMembers_CharId",
                        column: x => x.CharId,
                        principalTable: "ClanMembers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ClanCreationPayments_CharId_RequestId",
                table: "ClanCreationPayments",
                columns: new[] { "CharId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClanCreationPayments_ConsumedByClanId",
                table: "ClanCreationPayments",
                column: "ConsumedByClanId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanMemberInheritances_ClanId_InheritanceId",
                table: "ClanMemberInheritances",
                columns: new[] { "ClanId", "InheritanceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClanCreationPayments");

            migrationBuilder.DropTable(
                name: "ClanMemberInheritances");

            migrationBuilder.DropTable(
                name: "ClanInheritances");

            migrationBuilder.DropColumn(
                name: "CastleNodeId",
                table: "Clans");

            migrationBuilder.DropColumn(
                name: "JadeFunds",
                table: "Clans");

            migrationBuilder.DropColumn(
                name: "ActiveInheritanceId",
                table: "ClanMembers");
        }
    }
}
