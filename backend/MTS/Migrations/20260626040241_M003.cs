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
            migrationBuilder.CreateTable(
                name: "IapProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StoreProductId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    Store = table.Column<int>(type: "int", nullable: false),
                    Reward = table.Column<byte[]>(type: "blob", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IapProducts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IapPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Store = table.Column<int>(type: "int", nullable: false),
                    PurchaseTokenHash = table.Column<byte[]>(type: "varbinary(3072)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IapPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IapPurchases_IapProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "IapProducts",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IapProducts_StoreProductId_Store",
                table: "IapProducts",
                columns: new[] { "StoreProductId", "Store" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IapPurchases_ProductId",
                table: "IapPurchases",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_IapPurchases_Store_PurchaseTokenHash",
                table: "IapPurchases",
                columns: new[] { "Store", "PurchaseTokenHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IapPurchases");

            migrationBuilder.DropTable(
                name: "IapProducts");
        }
    }
}
