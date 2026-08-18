using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M004 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReleaseDailyRewardClaims_Characters_CharId",
                table: "ReleaseDailyRewardClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReleaseDailyRewardClaims",
                table: "ReleaseDailyRewardClaims");

            migrationBuilder.RenameTable(
                name: "ReleaseDailyRewardClaims",
                newName: "ReleaseDailyRewardClaim");

            migrationBuilder.UpdateData(
                table: "IapProducts",
                keyColumn: "Name",
                keyValue: null,
                column: "Name",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "IapProducts",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReleaseDailyRewardClaim",
                table: "ReleaseDailyRewardClaim",
                columns: new[] { "CharId", "RewardDay" });

            migrationBuilder.CreateTable(
                name: "DailyRewardMonthlyPasses",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Rewards = table.Column<byte[]>(type: "blob", nullable: false),
                    LastClaim = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRewardMonthlyPasses", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_DailyRewardMonthlyPasses_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_ReleaseDailyRewardClaim_Characters_CharId",
                table: "ReleaseDailyRewardClaim",
                column: "CharId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReleaseDailyRewardClaim_Characters_CharId",
                table: "ReleaseDailyRewardClaim");

            migrationBuilder.DropTable(
                name: "DailyRewardMonthlyPasses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReleaseDailyRewardClaim",
                table: "ReleaseDailyRewardClaim");

            migrationBuilder.RenameTable(
                name: "ReleaseDailyRewardClaim",
                newName: "ReleaseDailyRewardClaims");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "IapProducts",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReleaseDailyRewardClaims",
                table: "ReleaseDailyRewardClaims",
                columns: new[] { "CharId", "RewardDay" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReleaseDailyRewardClaims_Characters_CharId",
                table: "ReleaseDailyRewardClaims",
                column: "CharId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
