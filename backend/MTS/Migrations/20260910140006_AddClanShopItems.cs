using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class AddClanShopItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClanShopItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    OwnerCharId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ItemType = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanShopItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanShopItems_Characters_OwnerCharId",
                        column: x => x.OwnerCharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClanShopItems_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ClanShopItems_ClanId",
                table: "ClanShopItems",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanShopItems_OwnerCharId",
                table: "ClanShopItems",
                column: "OwnerCharId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClanShopItems");
        }
    }
}
