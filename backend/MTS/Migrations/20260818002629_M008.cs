using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class M008 : Migration
    {
        /// <inheritdoc />
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ============================
    // UseDans: WithOne -> WithMany
    // ============================

    migrationBuilder.DropForeignKey(
        name: "FK_UseDans_Characters_CharId",
        table: "UseDans");

    migrationBuilder.DropIndex(
        name: "IX_UseDans_CharId",
        table: "UseDans");

    migrationBuilder.CreateIndex(
        name: "IX_UseDans_CharId",
        table: "UseDans",
        column: "CharId");

    migrationBuilder.AddForeignKey(
        name: "FK_UseDans_Characters_CharId",
        table: "UseDans",
        column: "CharId",
        principalTable: "Characters",
        principalColumn: "Id",
        onDelete: ReferentialAction.Cascade);



    // =====================================
    // DailyRewardMonthlyPasses: CharId -> Id
    // =====================================

    migrationBuilder.DropForeignKey(
        name: "FK_DailyRewardMonthlyPasses_Characters_CharId",
        table: "DailyRewardMonthlyPasses");


    migrationBuilder.DropPrimaryKey(
        name: "PK_DailyRewardMonthlyPasses",
        table: "DailyRewardMonthlyPasses");


    migrationBuilder.AlterColumn<int>(
        name: "Id",
        table: "DailyRewardMonthlyPasses",
        type: "int",
        nullable: false,
        oldClrType: typeof(int),
        oldType: "int",
        oldNullable: true)
        .Annotation(
            "MySql:ValueGenerationStrategy",
            MySqlValueGenerationStrategy.IdentityColumn);


    migrationBuilder.AddPrimaryKey(
        name: "PK_DailyRewardMonthlyPasses",
        table: "DailyRewardMonthlyPasses",
        column: "Id");


    migrationBuilder.CreateIndex(
        name: "IX_DailyRewardMonthlyPasses_CharId",
        table: "DailyRewardMonthlyPasses",
        column: "CharId");


    migrationBuilder.AddForeignKey(
        name: "FK_DailyRewardMonthlyPasses_Characters_CharId",
        table: "DailyRewardMonthlyPasses",
        column: "CharId",
        principalTable: "Characters",
        principalColumn: "Id",
        onDelete: ReferentialAction.Cascade);
}

        /// <inheritdoc />
     protected override void Down(MigrationBuilder migrationBuilder)
{
    // =====================================
    // DailyRewardMonthlyPasses: Id -> CharId
    // =====================================

    migrationBuilder.DropForeignKey(
        name: "FK_DailyRewardMonthlyPasses_Characters_CharId",
        table: "DailyRewardMonthlyPasses");

    migrationBuilder.DropPrimaryKey(
        name: "PK_DailyRewardMonthlyPasses",
        table: "DailyRewardMonthlyPasses");

    migrationBuilder.DropIndex(
        name: "IX_DailyRewardMonthlyPasses_CharId",
        table: "DailyRewardMonthlyPasses");


    migrationBuilder.AlterColumn<int>(
        name: "Id",
        table: "DailyRewardMonthlyPasses",
        type: "int",
        nullable: true,
        oldClrType: typeof(int),
        oldType: "int")
        .OldAnnotation(
            "MySql:ValueGenerationStrategy",
            MySqlValueGenerationStrategy.IdentityColumn);


    migrationBuilder.AddPrimaryKey(
        name: "PK_DailyRewardMonthlyPasses",
        table: "DailyRewardMonthlyPasses",
        column: "CharId");


    migrationBuilder.AddForeignKey(
        name: "FK_DailyRewardMonthlyPasses_Characters_CharId",
        table: "DailyRewardMonthlyPasses",
        column: "CharId",
        principalTable: "Characters",
        principalColumn: "Id",
        onDelete: ReferentialAction.Cascade);



    // ==============================
    // UseDans: WithMany -> WithOne
    // ==============================

    migrationBuilder.DropForeignKey(
        name: "FK_UseDans_Characters_CharId",
        table: "UseDans");

    migrationBuilder.DropIndex(
        name: "IX_UseDans_CharId",
        table: "UseDans");


    migrationBuilder.CreateIndex(
        name: "IX_UseDans_CharId",
        table: "UseDans",
        column: "CharId",
        unique: true);


    migrationBuilder.AddForeignKey(
        name: "FK_UseDans_Characters_CharId",
        table: "UseDans",
        column: "CharId",
        principalTable: "Characters",
        principalColumn: "Id",
        onDelete: ReferentialAction.Cascade);
}
    }
}
