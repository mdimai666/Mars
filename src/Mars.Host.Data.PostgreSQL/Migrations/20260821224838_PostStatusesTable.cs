using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class PostStatusesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "post_status_list",
                table: "post_types");

            migrationBuilder.AddColumn<Guid>(
                name: "status_id",
                table: "posts",
                type: "uuid",
                nullable: true,
                comment: "ИД статуса");

            migrationBuilder.CreateTable(
                name: "post_statuses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    title = table.Column<string>(type: "text", nullable: false, comment: "Название"),
                    slug = table.Column<string>(type: "varchar(256)", nullable: false, comment: "Значение"),
                    color = table.Column<string>(type: "varchar(50)", nullable: false, comment: "Цвет (канбан)"),
                    order = table.Column<int>(type: "integer", nullable: false, comment: "Порядок (канбан)"),
                    post_type_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_post_statuses_post_types_post_type_id",
                        column: x => x.post_type_id,
                        principalTable: "post_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_posts_status_id",
                table: "posts",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_statuses_post_type_id_slug",
                table: "post_statuses",
                columns: new[] { "post_type_id", "slug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_posts_post_statuses_status_id",
                table: "posts",
                column: "status_id",
                principalTable: "post_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_posts_post_statuses_status_id",
                table: "posts");

            migrationBuilder.DropTable(
                name: "post_statuses");

            migrationBuilder.DropIndex(
                name: "ix_posts_status_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "status_id",
                table: "posts");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "posts",
                type: "varchar(256)",
                nullable: false,
                defaultValue: "",
                comment: "Статус");

            migrationBuilder.AddColumn<string>(
                name: "post_status_list",
                table: "post_types",
                type: "jsonb",
                nullable: true);
        }
    }
}
