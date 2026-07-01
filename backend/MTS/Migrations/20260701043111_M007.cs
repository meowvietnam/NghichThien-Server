using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M007 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ShopDuocLieu = table.Column<byte[]>(type: "blob", nullable: false),
                    ShopVatLieu = table.Column<byte[]>(type: "blob", nullable: false),
                    ShopSkill = table.Column<byte[]>(type: "blob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopManagers", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_ShopManagers_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopManagers");
        }
    }
}
