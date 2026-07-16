using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M019 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalGacha",
                table: "GachaManagers",
                newName: "GachaVatLieu");

            migrationBuilder.AddColumn<int>(
                name: "GachaCongPhap",
                table: "GachaManagers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GachaDanDuoc",
                table: "GachaManagers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GachaDuocLieu",
                table: "GachaManagers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GachaThanThong",
                table: "GachaManagers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GachaCongPhap",
                table: "GachaManagers");

            migrationBuilder.DropColumn(
                name: "GachaDanDuoc",
                table: "GachaManagers");

            migrationBuilder.DropColumn(
                name: "GachaDuocLieu",
                table: "GachaManagers");

            migrationBuilder.DropColumn(
                name: "GachaThanThong",
                table: "GachaManagers");

            migrationBuilder.RenameColumn(
                name: "GachaVatLieu",
                table: "GachaManagers",
                newName: "TotalGacha");
        }
    }
}
