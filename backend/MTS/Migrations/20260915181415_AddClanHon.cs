using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class AddClanHon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Hon",
                table: "Clans",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MaxHon",
                table: "Clans",
                type: "bigint",
                nullable: false,
                defaultValue: 100L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hon",
                table: "Clans");

            migrationBuilder.DropColumn(
                name: "MaxHon",
                table: "Clans");
        }
    }
}
