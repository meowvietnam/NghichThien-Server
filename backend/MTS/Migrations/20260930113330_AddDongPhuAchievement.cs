using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class AddDongPhuAchievement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DongPhuAchievementProgresses",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Stat = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    ClaimedTiers = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongPhuAchievementProgresses", x => new { x.CharId, x.Stat });
                    table.ForeignKey(
                        name: "FK_DongPhuAchievementProgresses_Characters_CharId",
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
                name: "DongPhuAchievementProgresses");
        }
    }
}
