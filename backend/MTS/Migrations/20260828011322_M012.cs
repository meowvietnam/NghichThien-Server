using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M012 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxLevel",
                table: "Progressions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DongPhus",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    LikeCount = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CurrentWateringHelpCount = table.Column<int>(type: "int", nullable: false),
                    LastReset = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongPhus", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_DongPhus_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DongPhuHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Log = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongPhuHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DongPhuHistories_DongPhus_CharId",
                        column: x => x.CharId,
                        principalTable: "DongPhus",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DongPhuInventories",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongPhuInventories", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_DongPhuInventories_DongPhus_CharId",
                        column: x => x.CharId,
                        principalTable: "DongPhus",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DongPhuLikes",
                columns: table => new
                {
                    OwnerCharId = table.Column<int>(type: "int", nullable: false),
                    OtherCharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongPhuLikes", x => new { x.OwnerCharId, x.OtherCharId });
                    table.ForeignKey(
                        name: "FK_DongPhuLikes_DongPhus_OwnerCharId",
                        column: x => x.OwnerCharId,
                        principalTable: "DongPhus",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DongPhuShops",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongPhuShops", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_DongPhuShops_DongPhus_CharId",
                        column: x => x.CharId,
                        principalTable: "DongPhus",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlantGardens",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantGardens", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_PlantGardens_DongPhus_CharId",
                        column: x => x.CharId,
                        principalTable: "DongPhus",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Seeds",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    HerbId = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seeds", x => new { x.CharId, x.HerbId });
                    table.ForeignKey(
                        name: "FK_Seeds_DongPhuInventories_CharId",
                        column: x => x.CharId,
                        principalTable: "DongPhuInventories",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SeedItemShops",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    HerbId = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PricePerSeed = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedItemShops", x => new { x.CharId, x.HerbId });
                    table.ForeignKey(
                        name: "FK_SeedItemShops_DongPhuShops_CharId",
                        column: x => x.CharId,
                        principalTable: "DongPhuShops",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlantingSlots",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    SlotIndex = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    IsLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UnlockLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantingSlots", x => new { x.CharId, x.SlotIndex });
                    table.ForeignKey(
                        name: "FK_PlantingSlots_PlantGardens_CharId",
                        column: x => x.CharId,
                        principalTable: "PlantGardens",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlantSeeds",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    SlotIndex = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    HerbId = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    PlantAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EstimateHarvestAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsHavest = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    WateringHelps = table.Column<byte[]>(type: "blob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSeeds", x => new { x.CharId, x.SlotIndex });
                    table.ForeignKey(
                        name: "FK_PlantSeeds_PlantGardens_CharId",
                        column: x => x.CharId,
                        principalTable: "PlantGardens",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlantSeeds_PlantingSlots_CharId_SlotIndex",
                        columns: x => new { x.CharId, x.SlotIndex },
                        principalTable: "PlantingSlots",
                        principalColumns: new[] { "CharId", "SlotIndex" },
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DongPhuHistories_CharId",
                table: "DongPhuHistories",
                column: "CharId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DongPhuHistories");

            migrationBuilder.DropTable(
                name: "DongPhuLikes");

            migrationBuilder.DropTable(
                name: "PlantSeeds");

            migrationBuilder.DropTable(
                name: "SeedItemShops");

            migrationBuilder.DropTable(
                name: "Seeds");

            migrationBuilder.DropTable(
                name: "PlantingSlots");

            migrationBuilder.DropTable(
                name: "DongPhuShops");

            migrationBuilder.DropTable(
                name: "DongPhuInventories");

            migrationBuilder.DropTable(
                name: "PlantGardens");

            migrationBuilder.DropTable(
                name: "DongPhus");

            migrationBuilder.DropColumn(
                name: "MaxLevel",
                table: "Progressions");
        }
    }
}
