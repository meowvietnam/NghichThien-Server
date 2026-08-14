using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases");

            migrationBuilder.AddForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases",
                column: "CharId",
                principalTable: "Characters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases");

            migrationBuilder.AddForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases",
                column: "CharId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
