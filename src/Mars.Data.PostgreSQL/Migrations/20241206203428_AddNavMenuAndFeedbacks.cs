using System;
using System.Collections.Generic;
using Mars.Host.Data.OwnedTypes.NavMenus;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddNavMenuAndFeedbacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feedbacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    title = table.Column<string>(type: "text", maxLength: 256, nullable: false, comment: "Название"),
                    content = table.Column<string>(type: "text", maxLength: 256, nullable: true, comment: "Текст"),
                    phone = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, comment: "Телефон"),
                    filled_username = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, comment: "Заполненное имя"),
                    email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, comment: "Email"),
                    feedback_type = table.Column<int>(type: "integer", nullable: false, comment: "Тип обратной связи"),
                    authorized_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "fk_feedbacks_users_authorized_user_id",
                        column: x => x.authorized_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                },
                comment: "Обратная связь");

            migrationBuilder.CreateTable(
                name: "nav_menus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    title = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Название"),
                    slug = table.Column<string>(type: "varchar(256)", nullable: false, comment: "slug"),
                    menu_items = table.Column<List<NavMenuItem>>(type: "jsonb", nullable: false, comment: "Элементы"),
                    @class = table.Column<string>(name: "class", type: "varchar(256)", nullable: false, comment: "Class"),
                    style = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Style"),
                    roles = table.Column<List<string>>(type: "varchar(128)[]", nullable: false, comment: "Роли"),
                    roles_inverse = table.Column<bool>(type: "boolean", nullable: false, comment: "Не для ролей"),
                    disabled = table.Column<bool>(type: "boolean", nullable: false, comment: "Отключен"),
                    tags = table.Column<List<string>>(type: "varchar(128)[]", nullable: false, comment: "Теги")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nav_menus", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_feedbacks_authorized_user_id",
                table: "feedbacks",
                column: "authorized_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feedbacks");

            migrationBuilder.DropTable(
                name: "nav_menus");
        }
    }
}
