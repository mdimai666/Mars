using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations {
    /// <inheritdoc />
    public partial class AddMediaFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "folder_id",
                table: "files",
                type: "uuid",
                nullable: true,
                comment: "ИД папки медиа");

            migrationBuilder.CreateTable(
                name: "media_folders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    name = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Имя папки"),
                    path = table.Column<string>(type: "text", maxLength: 2048, nullable: false, comment: "Физический путь папки от upload"),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "ИД родительской папки"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД пользователя, создавшего папку"),
                    icon = table.Column<string>(type: "varchar(256)", nullable: true, comment: "Значок папки (зарезервировано)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_folders", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_folders_media_folders_parent_id",
                        column: x => x.parent_id,
                        principalTable: "media_folders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_files_folder_id",
                table: "files",
                column: "folder_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_folders_parent_id",
                table: "media_folders",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_folders_path",
                table: "media_folders",
                column: "path",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_files_media_folders_folder_id",
                table: "files",
                column: "folder_id",
                principalTable: "media_folders",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Data-миграция: зарегистрировать существующие каталоги как папки
            // (legacy-файлы лежат в Media/{год}) и привязать к ним файлы.
            // created_by = пустой Guid — папки созданы системой, а не пользователем.
            migrationBuilder.Sql(@"
WITH RECURSIVE dirs(path) AS (
    SELECT DISTINCT regexp_replace(file_physical_path, '/[^/]+$', '')
    FROM files
    WHERE file_physical_path LIKE '%/%'
    UNION
    SELECT regexp_replace(d.path, '/[^/]+$', '')
    FROM dirs d
    WHERE d.path LIKE '%/%'
)
INSERT INTO media_folders (id, created_at, name, path, created_by)
SELECT gen_random_uuid(), now(), regexp_replace(d.path, '^.*/', ''), d.path, '00000000-0000-0000-0000-000000000000'
FROM dirs d
WHERE d.path LIKE '%/%';
");

            // Родитель для вложенных папок (верхний уровень Media/... остаётся без родителя)
            migrationBuilder.Sql(@"
UPDATE media_folders f
SET parent_id = p.id
FROM media_folders p
WHERE f.path LIKE '%/%/%'
  AND p.path = regexp_replace(f.path, '/[^/]+$', '');
");

            migrationBuilder.Sql(@"
UPDATE files
SET folder_id = mf.id
FROM media_folders mf
WHERE mf.path = regexp_replace(files.file_physical_path, '/[^/]+$', '');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_files_media_folders_folder_id",
                table: "files");

            migrationBuilder.DropTable(
                name: "media_folders");

            migrationBuilder.DropIndex(
                name: "ix_files_folder_id",
                table: "files");

            migrationBuilder.DropColumn(
                name: "folder_id",
                table: "files");
        }
    }
}
