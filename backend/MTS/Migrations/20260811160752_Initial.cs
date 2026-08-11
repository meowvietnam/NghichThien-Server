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
                name: "AuctionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Sessions = table.Column<byte[]>(type: "blob", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionHistories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GiftCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rewards = table.Column<byte[]>(type: "blob", nullable: false),
                    CanUse = table.Column<int>(type: "int", nullable: false),
                    TotalUsed = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpireDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCodes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    ActualProductType = table.Column<int>(type: "int", nullable: false),
                    Store = table.Column<int>(type: "int", nullable: false),
                    Reward = table.Column<byte[]>(type: "blob", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IapProducts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Store = table.Column<int>(type: "int", nullable: false),
                    PromotionApplicable = table.Column<byte[]>(type: "blob", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
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
                    PurchaseTokenHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    MoneyVipSnapshot = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_0900_as_cs")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonPhai = table.Column<int>(type: "int", nullable: false),
                    MoneyVip = table.Column<int>(type: "int", nullable: false),
                    IsInfiniteEnergy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsNoAds = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CanFly = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CurrentFreeAdsMoneyVipCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                name: "GoogleAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GuestAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IPAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestAccounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LocalAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_bin")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalAccounts_Users_UserId",
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
                name: "ArenaManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    CurrentArenaCount = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArenaManagers", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_ArenaManagers_Characters_CharId",
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
                name: "EquipmentManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    DonThuatUsed = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu1Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu2Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu3Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhuTu4Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ChuTu1Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ChuTu2Used = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    VuKhi = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PhongCu = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    TrangSuc = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentManagers", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_EquipmentManagers_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GachaManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    GachaThanThong = table.Column<int>(type: "int", nullable: false),
                    GachaCongPhap = table.Column<int>(type: "int", nullable: false),
                    GachaDanDuoc = table.Column<int>(type: "int", nullable: false),
                    GachaDuocLieu = table.Column<int>(type: "int", nullable: false),
                    GachaVatLieu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GachaManagers", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_GachaManagers_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GiftcodeUseds",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    CodeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftcodeUseds", x => new { x.CharId, x.CodeId });
                    table.ForeignKey(
                        name: "FK_GiftcodeUseds_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiftcodeUseds_GiftCodes_CodeId",
                        column: x => x.CodeId,
                        principalTable: "GiftCodes",
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
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseId = table.Column<int>(type: "int", nullable: true),
                    Link = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_IapProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "IapProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_IapPurchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "IapPurchases",
                        principalColumn: "Id");
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
                name: "QuestManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    CurrentMainQuest = table.Column<int>(type: "int", nullable: false),
                    CurrentCountSideQuest = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestManagers", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_QuestManagers_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReleaseDailyRewardClaims",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    RewardDay = table.Column<int>(type: "int", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseDailyRewardClaims", x => new { x.CharId, x.RewardDay });
                    table.ForeignKey(
                        name: "FK_ReleaseDailyRewardClaims_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    ShopDuocLieu = table.Column<byte[]>(type: "blob", nullable: false),
                    ShopVatLieu = table.Column<byte[]>(type: "blob", nullable: false),
                    ShopSkill = table.Column<byte[]>(type: "blob", nullable: false),
                    ShopSkillAll = table.Column<byte[]>(type: "blob", nullable: false)
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
                name: "SpendingMoneyVips",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    SpendFor = table.Column<int>(type: "int", nullable: false),
                    MoneyVip = table.Column<long>(type: "bigint", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpendingMoneyVips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpendingMoneyVips_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RankRewards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsClaim = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankRewards_ArenaManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "ArenaManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DanCombatUseds",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Count = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanCombatUseds", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_DanCombatUseds_EquipmentManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "EquipmentManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThanThongEquips",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    SlotIndex = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanThongEquips", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_ThanThongEquips_EquipmentManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "EquipmentManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CamNgos",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CamNgos", x => new { x.CharId, x.Id });
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
                    table.PrimaryKey("PK_CongPhaps", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_CongPhaps_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    SelfId = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Effects = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaterialIds = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => new { x.CharId, x.SelfId, x.Id });
                    table.ForeignKey(
                        name: "FK_Equipments_InventoryManagers_CharId",
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
                    SelfId = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Durability = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Furnaces", x => new { x.CharId, x.SelfId, x.Id });
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
                    table.PrimaryKey("PK_NgoDaos", x => new { x.CharId, x.Id });
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
                    table.PrimaryKey("PK_ThanThongs", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_ThanThongs_InventoryManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "InventoryManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DailyQuests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    EDailyQuest = table.Column<int>(type: "int", nullable: false),
                    QuestReward = table.Column<byte[]>(type: "blob", nullable: false),
                    IsRewardClaimed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastReload = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyQuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyQuests_QuestManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "QuestManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsClaim = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Rewards = table.Column<byte[]>(type: "blob", nullable: false),
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
                    table.PrimaryKey("PK_Relationships", x => new { x.CharId, x.Npc });
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
                name: "IX_DailyQuests_CharId",
                table: "DailyQuests",
                column: "CharId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodes_Code",
                table: "GiftCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftcodeUseds_CodeId",
                table: "GiftcodeUseds",
                column: "CodeId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAccounts_Email",
                table: "GoogleAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleAccounts_UserId",
                table: "GoogleAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestAccounts_UserId",
                table: "GuestAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestAccounts_Username",
                table: "GuestAccounts",
                column: "Username",
                unique: true);

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
                name: "IX_LocalAccounts_UserId",
                table: "LocalAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalAccounts_Username",
                table: "LocalAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mails_CharId",
                table: "Mails",
                column: "CharId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CharId",
                table: "Orders",
                column: "CharId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ProductId",
                table: "Orders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PurchaseId",
                table: "Orders",
                column: "PurchaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RankRewards_CharId",
                table: "RankRewards",
                column: "CharId");

            migrationBuilder.CreateIndex(
                name: "IX_SpendingMoneyVips_CharId",
                table: "SpendingMoneyVips",
                column: "CharId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appearances");

            migrationBuilder.DropTable(
                name: "AuctionHistories");

            migrationBuilder.DropTable(
                name: "CamNgos");

            migrationBuilder.DropTable(
                name: "CharacterStats");

            migrationBuilder.DropTable(
                name: "CongPhaps");

            migrationBuilder.DropTable(
                name: "DailyQuests");

            migrationBuilder.DropTable(
                name: "DanCombatUseds");

            migrationBuilder.DropTable(
                name: "Equipments");

            migrationBuilder.DropTable(
                name: "Furnaces");

            migrationBuilder.DropTable(
                name: "GachaManagers");

            migrationBuilder.DropTable(
                name: "GiftcodeUseds");

            migrationBuilder.DropTable(
                name: "GoogleAccounts");

            migrationBuilder.DropTable(
                name: "GuestAccounts");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "LocalAccounts");

            migrationBuilder.DropTable(
                name: "Mails");

            migrationBuilder.DropTable(
                name: "NgoDaos");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Progressions");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "RankRewards");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "ReleaseDailyRewardClaims");

            migrationBuilder.DropTable(
                name: "ShopManagers");

            migrationBuilder.DropTable(
                name: "SpendingMoneyVips");

            migrationBuilder.DropTable(
                name: "ThanThongEquips");

            migrationBuilder.DropTable(
                name: "ThanThongs");

            migrationBuilder.DropTable(
                name: "QuestManagers");

            migrationBuilder.DropTable(
                name: "GiftCodes");

            migrationBuilder.DropTable(
                name: "IapPurchases");

            migrationBuilder.DropTable(
                name: "ArenaManagers");

            migrationBuilder.DropTable(
                name: "Socials");

            migrationBuilder.DropTable(
                name: "EquipmentManagers");

            migrationBuilder.DropTable(
                name: "InventoryManagers");

            migrationBuilder.DropTable(
                name: "IapProducts");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
