using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M008 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyQuests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    EDailyQuest = table.Column<int>(type: "int", nullable: false),
                    QuestReward = table.Column<byte[]>(type: "blob", nullable: false),
                    IsRewardClaimed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastReload = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyQuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyQuests_QuestManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "QuestManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DailyQuests_CharId",
                table: "DailyQuests",
                column: "CharId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyQuests");
        }
    }
}
