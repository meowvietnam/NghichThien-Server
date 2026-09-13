using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class RenameClanInheritanceEffectBuffTypeToEffectType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BuffType",
                table: "ClanInheritanceEffects",
                newName: "EffectType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EffectType",
                table: "ClanInheritanceEffects",
                newName: "BuffType");
        }
    }
}
