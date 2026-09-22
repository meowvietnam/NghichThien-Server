using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class AddBossProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BossProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    BossBytes = table.Column<byte[]>(type: "longblob", nullable: false),
                    Hp = table.Column<int>(type: "int", nullable: false),
                    AttemptsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KillerId = table.Column<int>(type: "int", nullable: true),
                    ParticipantRewards = table.Column<byte[]>(type: "blob", nullable: false),
                    KillerRewards = table.Column<byte[]>(type: "blob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossProgresses", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BossProgresses");
        }
    }
}
