using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class PersistFreeClanContributionPerCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastFreeClanContributionAt",
                table: "Characters",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Giữ lại trạng thái lượt miễn phí của các thành viên hiện tại khi chuyển nguồn
            // dữ liệu từ ClanMembers sang Characters.
            migrationBuilder.Sql(
                """
                UPDATE `Characters` AS `character`
                INNER JOIN (
                    SELECT `CharId`, MAX(`LastFreeContributionAt`) AS `LastFreeContributionAt`
                    FROM `ClanMembers`
                    GROUP BY `CharId`
                ) AS `member` ON `member`.`CharId` = `character`.`Id`
                SET `character`.`LastFreeClanContributionAt` = `member`.`LastFreeContributionAt`;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFreeClanContributionAt",
                table: "Characters");
        }
    }
}
