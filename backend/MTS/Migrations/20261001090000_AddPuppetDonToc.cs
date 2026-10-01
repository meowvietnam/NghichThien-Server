using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MTS.Data;

#nullable disable

namespace MTS.Migrations
{
    /// <summary>Thêm cột Độn tốc cho rối Tông môn chiến (viết tay, không cần Designer).</summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261001090000_AddPuppetDonToc")]
    public partial class AddPuppetDonToc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DonToc",
                table: "Puppets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonToc",
                table: "Puppets");
        }
    }
}
