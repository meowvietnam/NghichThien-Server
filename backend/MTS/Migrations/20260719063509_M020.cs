using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M020 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ShopSkillAll",
                table: "ShopManagers",
                type: "blob",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShopSkillAll",
                table: "ShopManagers");
        }
    }
}
