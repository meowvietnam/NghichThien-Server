using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M002_ReleaseDailyRewardClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReleaseDailyRewardClaims",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    RewardDay = table.Column<int>(type: "int", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseDailyRewardClaims", x => new { x.CharId, x.RewardDay });
                    table.ForeignKey(
                        name: "FK_ReleaseDailyRewardClaims_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseDailyRewardClaims");
        }
    }
}
