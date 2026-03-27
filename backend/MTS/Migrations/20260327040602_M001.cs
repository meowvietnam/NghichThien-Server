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
            migrationBuilder.AddColumn<int>(
                name: "Hp",
                table: "CharacterStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CamNgoSaves",
                table: "Characters",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ChuTu1Used",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChuTu2Used",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CongPhapOwner",
                table: "Characters",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DanCombatUsed",
                table: "Characters",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DonThuatUsed",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MonPhai",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NgoDaoDiem",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NgoDaoSaves",
                table: "Characters",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PhuTu1Used",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhuTu2Used",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhuTu3Used",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhuTu4Used",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThanThongEquip",
                table: "Characters",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ThanThongOwner",
                table: "Characters",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hp",
                table: "CharacterStats");

            migrationBuilder.DropColumn(
                name: "CamNgoSaves",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ChuTu1Used",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ChuTu2Used",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CongPhapOwner",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DanCombatUsed",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "DonThuatUsed",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "MonPhai",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "NgoDaoDiem",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "NgoDaoSaves",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PhuTu1Used",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PhuTu2Used",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PhuTu3Used",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PhuTu4Used",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ThanThongEquip",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ThanThongOwner",
                table: "Characters");
        }
    }
}
