using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WelcomeRewards",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ClaimCount = table.Column<int>(type: "int", nullable: false),
                    ClaimDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelcomeRewards", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_WelcomeRewards_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IapPurchases_CharId",
                table: "IapPurchases",
                column: "CharId");

            migrationBuilder.AddForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases",
                column: "CharId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases");

            migrationBuilder.DropTable(
                name: "WelcomeRewards");

            migrationBuilder.DropIndex(
                name: "IX_IapPurchases_CharId",
                table: "IapPurchases");
        }
    }
}
