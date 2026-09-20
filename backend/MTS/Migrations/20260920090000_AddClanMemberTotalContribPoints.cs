using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    public partial class AddClanMemberTotalContribPoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TotalContribPoints",
                table: "ClanMembers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Historical spending cannot be reconstructed; seed existing memberships
            // from their current balance. New memberships start at zero.
            migrationBuilder.Sql("UPDATE `ClanMembers` SET `TotalContribPoints` = `ContribPoints`;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TotalContribPoints", table: "ClanMembers");
        }
    }
}
