using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M011 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "GachaCongPhapDict",
                table: "GachaManagers",
                type: "blob",
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "GachaThanThongDict",
                table: "GachaManagers",
                type: "blob",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GachaCongPhapDict",
                table: "GachaManagers");

            migrationBuilder.DropColumn(
                name: "GachaThanThongDict",
                table: "GachaManagers");
        }
    }
}
