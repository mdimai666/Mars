using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    name = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Название"),
                    key_hash = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Хэш секретной части ключа (SHA-256, base64)"),
                    key_prefix = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Префикс ключа для отображения"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Действителен до"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_api_keys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "API ключ пользователя");

            migrationBuilder.CreateIndex(
                name: "ix_user_api_keys_user_id",
                table: "user_api_keys",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_api_keys");
        }
    }
}
