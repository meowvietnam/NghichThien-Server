using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M014 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Points",
                table: "Relationships",
                type: "int",
                nullable: false,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned");

            migrationBuilder.AlterColumn<int>(
                name: "Count",
                table: "Items",
                type: "int",
                nullable: false,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned");

            migrationBuilder.CreateTable(
                name: "EventManagers",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventManagers", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_EventManagers_Characters_CharId",
                        column: x => x.CharId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EventInventories",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInventories", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_EventInventories_EventManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "EventManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GameEvent = table.Column<byte[]>(type: "blob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_Events_EventManagers_CharId",
                        column: x => x.CharId,
                        principalTable: "EventManagers",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ItemEvents",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEvents", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_ItemEvents_EventInventories_CharId",
                        column: x => x.CharId,
                        principalTable: "EventInventories",
                        principalColumn: "CharId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "ItemEvents");

            migrationBuilder.DropTable(
                name: "EventInventories");

            migrationBuilder.DropTable(
                name: "EventManagers");

            migrationBuilder.AlterColumn<ushort>(
                name: "Points",
                table: "Relationships",
                type: "smallint unsigned",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<ushort>(
                name: "Count",
                table: "Items",
                type: "smallint unsigned",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
