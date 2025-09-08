using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PluginExample.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "PackageName.PluginExample");

            migrationBuilder.CreateTable(
                name: "news",
                schema: "PackageName.PluginExample",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    title = table.Column<string>(type: "text", maxLength: 500, nullable: false, comment: "Название"),
                    content = table.Column<string>(type: "text", maxLength: 2000, nullable: true, comment: "Текст"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД пользователя")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news", x => x.id);
                    table.ForeignKey(
                        name: "fk_news_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_news_user_id",
                schema: "PackageName.PluginExample",
                table: "news",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "news",
                schema: "PackageName.PluginExample");
        }
    }
}
