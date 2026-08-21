using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class MetaFieldOwnerComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "hidden",
                table: "meta_fields",
                type: "boolean",
                nullable: false,
                comment: "Скрытое: хранится и отдаётся в API, но скрыт в формах",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Скрытое");

            migrationBuilder.AlterColumn<bool>(
                name: "disabled",
                table: "meta_fields",
                type: "boolean",
                nullable: false,
                comment: "Отключен: исключён из генерации/форм, значения сохраняются",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Отключен");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "hidden",
                table: "meta_fields",
                type: "boolean",
                nullable: false,
                comment: "Скрытое",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Скрытое: хранится и отдаётся в API, но скрыт в формах");

            migrationBuilder.AlterColumn<bool>(
                name: "disabled",
                table: "meta_fields",
                type: "boolean",
                nullable: false,
                comment: "Отключен",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Отключен: исключён из генерации/форм, значения сохраняются");
        }
    }
}
