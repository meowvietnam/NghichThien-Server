using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M017 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastShopItemAddedAt",
                table: "ClanMembers");

            migrationBuilder.DropColumn(
                name: "ShopItemsAddedToday",
                table: "ClanMembers");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastShopItemAddedAt",
                table: "Clans",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ShopItemsAddedToday",
                table: "Clans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastShopItemAddedAt",
                table: "Clans");

            migrationBuilder.DropColumn(
                name: "ShopItemsAddedToday",
                table: "Clans");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastShopItemAddedAt",
                table: "ClanMembers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ShopItemsAddedToday",
                table: "ClanMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
