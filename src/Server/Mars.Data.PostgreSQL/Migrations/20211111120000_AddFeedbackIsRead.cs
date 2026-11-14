using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackIsRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Исторические обращения считаем прочитанными: колонка добавляется с DEFAULT true
            // (backfill существующих строк), затем дефолт снимается — модель не объявляет
            // HasDefaultValue, и EF всегда пишет IsRead явно (новые строки = false).
            migrationBuilder.AddColumn<bool>(
                name: "is_read",
                table: "feedbacks",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Прочитано");

            migrationBuilder.Sql("ALTER TABLE feedbacks ALTER COLUMN is_read DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_read",
                table: "feedbacks");
        }
    }
}