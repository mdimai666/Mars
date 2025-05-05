using System;
using Mars.Host.Data.OwnedTypes.Files;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    file_name = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Имя файла"),
                    file_physical_path = table.Column<string>(type: "text", maxLength: 2048, nullable: false, comment: "Физический путь файла от upload"),
                    file_virtual_path = table.Column<string>(type: "text", maxLength: 2048, nullable: false, comment: "Виртуальный путь файла"),
                    file_size = table.Column<decimal>(type: "numeric(20,0)", nullable: false, comment: "Размер файла в байтах"),
                    file_ext = table.Column<string>(type: "varchar(20)", nullable: false, comment: "Расширение файла. Без ведущей точки."),
                    meta = table.Column<FileEntityMeta>(type: "jsonb", nullable: false, comment: "Мета поля"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_files_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    key = table.Column<string>(type: "varchar(256)", nullable: false),
                    type = table.Column<string>(type: "varchar(256)", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_options", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_files_user_id",
                table: "files",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "options");
        }
    }
}
