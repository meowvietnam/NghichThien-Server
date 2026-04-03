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
            migrationBuilder.AddColumn<string>(
                name: "CongPhaps",
                table: "InventoryManagers",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LinhDans",
                table: "InventoryManagers",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhongCus",
                table: "InventoryManagers",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ThanThongs",
                table: "InventoryManagers",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TrangSucs",
                table: "InventoryManagers",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VuKhis",
                table: "InventoryManagers",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CongPhaps",
                table: "InventoryManagers");

            migrationBuilder.DropColumn(
                name: "LinhDans",
                table: "InventoryManagers");

            migrationBuilder.DropColumn(
                name: "PhongCus",
                table: "InventoryManagers");

            migrationBuilder.DropColumn(
                name: "ThanThongs",
                table: "InventoryManagers");

            migrationBuilder.DropColumn(
                name: "TrangSucs",
                table: "InventoryManagers");

            migrationBuilder.DropColumn(
                name: "VuKhis",
                table: "InventoryManagers");
        }
    }
}
