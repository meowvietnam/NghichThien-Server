using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MTS.Migrations
{
    /// <inheritdoc />
    public partial class ChangeClanInheritanceEffectTypeToNullableByte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cột EffectType trước đây lưu giá trị enum EBuffAndDeBuff (int, có None = 0).
            // Nay đổi sang lưu enum EffectType (MTS.Game.LuyenKhi, byte, không có None) dưới dạng nullable.
            // Hai enum không có ánh xạ 1-1 nên các giá trị số cũ không còn đúng ý nghĩa sau khi đổi kiểu;
            // để tránh âm thầm sai lệch dữ liệu, dữ liệu cũ trong cột này bị xóa (đặt NULL) sau khi đổi kiểu.
            // Người quản lý tông môn sẽ cần thiết lập lại hiệu ứng truyền thừa cho các ô đã có.
            migrationBuilder.AlterColumn<byte?>(
                name: "EffectType",
                table: "ClanInheritanceEffects",
                type: "tinyint unsigned",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("UPDATE `ClanInheritanceEffects` SET `EffectType` = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE `ClanInheritanceEffects` SET `EffectType` = 0 WHERE `EffectType` IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "EffectType",
                table: "ClanInheritanceEffects",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(byte?),
                oldType: "tinyint unsigned",
                oldNullable: true);
        }
    }
}
