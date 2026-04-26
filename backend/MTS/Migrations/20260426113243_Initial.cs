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
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonPhai = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                name: "Appearances",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Skins = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Colors = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appearances", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_Appearances_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
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
                name: "Equipments",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    DonThuatUsed = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu1Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu2Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu3Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu4Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ChuTu1Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ChuTu2Used = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_Equipments_Characters_CharId",
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
                    CharId = table.Column<int>(type: "int", nullable: false)
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
                name: "Progressions",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CurrentExp = table.Column<int>(type: "int", nullable: false),
                    CurrentEnergy = table.Column<int>(type: "int", nullable: false),
                    NgoDaoDiem = table.Column<int>(type: "int", nullable: false),
                    IdCurrentNode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Progressions", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_Progressions_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Socials",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Socials", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_Socials_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DanCombatUseds",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanCombatUseds", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_DanCombatUseds_Equipments_CharId",
                        column: x => x.CharId,
                        principalTable: "Equipments",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThanThongEquips",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanThongEquips", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_ThanThongEquips_Equipments_CharId",
                        column: x => x.CharId,
                        principalTable: "Equipments",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CamNgos",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Value = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CamNgos", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_CamNgos_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CongPhaps",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Level = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongPhaps", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_CongPhaps_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Furnaces",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    SelfId = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Durability = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Furnaces", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_Furnaces_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Count = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => new { x.CharId, x.Id, x.Type });
                    table.ForeignKey(
                        name: "FK_Items_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NgoDaos",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NgoDaos", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_NgoDaos_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThanThongs",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanThongs", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_ThanThongs_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
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
                        name: "FK_Mails_Socials_CharId",
                        column: x => x.CharId,
                        principalTable: "Socials",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Npc = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Points = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_Relationships_Socials_CharId",
                        column: x => x.CharId,
                        principalTable: "Socials",
                        principalColumn: "CharId",
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
                name: "Appearances");

            migrationBuilder.DropTable(
                name: "CamNgos");

            migrationBuilder.DropTable(
                name: "CharacterStats");

            migrationBuilder.DropTable(
                name: "CongPhaps");

            migrationBuilder.DropTable(
                name: "DanCombatUseds");

            migrationBuilder.DropTable(
                name: "DeveloperAccounts");

            migrationBuilder.DropTable(
                name: "Furnaces");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Mails");

            migrationBuilder.DropTable(
                name: "NgoDaos");

            migrationBuilder.DropTable(
                name: "Progressions");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "ThanThongEquips");

            migrationBuilder.DropTable(
                name: "ThanThongs");

            migrationBuilder.DropTable(
                name: "Socials");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "InventoryManagers");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
