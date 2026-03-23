using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CurrentExp = table.Column<int>(type: "int", nullable: false),
                    Skins = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Colors = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeveloperAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeveloperAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeveloperAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CharacterStats",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Kim = table.Column<float>(type: "float", nullable: false),
                    Moc = table.Column<float>(type: "float", nullable: false),
                    Thuy = table.Column<float>(type: "float", nullable: false),
                    Hoa = table.Column<float>(type: "float", nullable: false),
                    Tho = table.Column<float>(type: "float", nullable: false),
                    Ma = table.Column<float>(type: "float", nullable: false),
                    TuChat = table.Column<int>(type: "int", nullable: false),
                    NgoTinh = table.Column<int>(type: "int", nullable: false),
                    ThanThuc = table.Column<int>(type: "int", nullable: false),
                    TocDoBay = table.Column<int>(type: "int", nullable: false),
                    LinhThach = table.Column<int>(type: "int", nullable: false),
                    SinhMenh = table.Column<int>(type: "int", nullable: false),
                    ThoNguyen = table.Column<int>(type: "int", nullable: false),
                    TamCanh = table.Column<int>(type: "int", nullable: false),
                    LinhCan = table.Column<int>(type: "int", nullable: false),
                    Ability = table.Column<int>(type: "int", nullable: false),
                    NgoDao = table.Column<int>(type: "int", nullable: false),
                    DanDoc = table.Column<int>(type: "int", nullable: false),
                    TuoiTac = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStats", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_CharacterStats_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId",
                table: "Characters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperAccounts_UserId",
                table: "DeveloperAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperAccounts_UserName",
                table: "DeveloperAccounts",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterStats");

            migrationBuilder.DropTable(
                name: "DeveloperAccounts");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
