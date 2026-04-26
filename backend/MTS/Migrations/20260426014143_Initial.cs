using System;
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
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IsInfiniteEnergy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                    MonPhai = table.Column<int>(type: "int", nullable: false),
                    NgoDaoSaves = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CamNgoSaves = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgoDaoDiem = table.Column<int>(type: "int", nullable: false),
                    ThanThongOwner = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThanThongEquip = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CongPhapOwner = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DonThuatUsed = table.Column<int>(type: "int", nullable: false),
                    PhuTu1Used = table.Column<int>(type: "int", nullable: false),
                    PhuTu2Used = table.Column<int>(type: "int", nullable: false),
                    PhuTu3Used = table.Column<int>(type: "int", nullable: false),
                    PhuTu4Used = table.Column<int>(type: "int", nullable: false),
                    ChuTu1Used = table.Column<int>(type: "int", nullable: false),
                    ChuTu2Used = table.Column<int>(type: "int", nullable: false),
                    DanCombatUsed = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdCurrentNode = table.Column<int>(type: "int", nullable: false),
                    CurrentEnergy = table.Column<int>(type: "int", nullable: false),
                    RelationShip = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    Hp = table.Column<int>(type: "int", nullable: false),
                    SinhMenh = table.Column<int>(type: "int", nullable: false),
                    ThoNguyen = table.Column<int>(type: "int", nullable: false),
                    TamCanh = table.Column<int>(type: "int", nullable: false),
                    LinhCan = table.Column<int>(type: "int", nullable: false),
                    Ability = table.Column<int>(type: "int", nullable: false),
                    DanDoc = table.Column<int>(type: "int", nullable: false),
                    TuoiTac = table.Column<long>(type: "bigint", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "InventoryManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Furnaces = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Recipes = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Herbs = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThanThongs = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CongPhaps = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VuKhis = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhongCus = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangSucs = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinhDans = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryManagers", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_InventoryManagers_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsClaim = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Rewards = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mails_Characters_CharId",
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

            migrationBuilder.CreateIndex(
                name: "IX_Mails_CharId",
                table: "Mails",
                column: "CharId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterStats");

            migrationBuilder.DropTable(
                name: "DeveloperAccounts");

            migrationBuilder.DropTable(
                name: "InventoryManagers");

            migrationBuilder.DropTable(
                name: "Mails");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
