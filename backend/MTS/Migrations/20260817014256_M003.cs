using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M003 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_IapPurchases_IapProducts_ProductId",
                table: "IapPurchases");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "IapPurchases",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CharId",
                table: "IapPurchases",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "LinkerId",
                table: "IapProducts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductLinkers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanBuyAgain = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NextDayCanBuy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLinkers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IapProducts_LinkerId",
                table: "IapProducts",
                column: "LinkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_IapProducts_ProductLinkers_LinkerId",
                table: "IapProducts",
                column: "LinkerId",
                principalTable: "ProductLinkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases",
                column: "CharId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IapPurchases_IapProducts_ProductId",
                table: "IapPurchases",
                column: "ProductId",
                principalTable: "IapProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IapProducts_ProductLinkers_LinkerId",
                table: "IapProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases");

            migrationBuilder.DropForeignKey(
                name: "FK_IapPurchases_IapProducts_ProductId",
                table: "IapPurchases");

            migrationBuilder.DropTable(
                name: "ProductLinkers");

            migrationBuilder.DropIndex(
                name: "IX_IapProducts_LinkerId",
                table: "IapProducts");

            migrationBuilder.DropColumn(
                name: "LinkerId",
                table: "IapProducts");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "IapPurchases",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CharId",
                table: "IapPurchases",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IapPurchases_Characters_CharId",
                table: "IapPurchases",
                column: "CharId",
                principalTable: "Characters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IapPurchases_IapProducts_ProductId",
                table: "IapPurchases",
                column: "ProductId",
                principalTable: "IapProducts",
                principalColumn: "Id");
        }
    }
}
