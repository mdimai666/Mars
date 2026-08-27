using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "post_category_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "Название"),
                    type_name = table.Column<string>(type: "varchar(128)", maxLength: 100, nullable: false, comment: "Тип"),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, comment: "Теги")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_category_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    post_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    root_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    post_category_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    slug = table.Column<string>(type: "varchar(256)", nullable: false, comment: "slug"),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, comment: "Теги"),
                    title = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false, comment: "Название"),
                    path = table.Column<string>(type: "varchar(2048)", nullable: false),
                    slug_path = table.Column<string>(type: "varchar(2048)", nullable: false),
                    path_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    levels_count = table.Column<int>(type: "integer", nullable: false),
                    disabled = table.Column<bool>(type: "boolean", nullable: false, comment: "Отключен")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_post_categories_post_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "post_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_post_categories_post_categories_root_id",
                        column: x => x.root_id,
                        principalTable: "post_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_categories_post_category_types_post_category_type_id",
                        column: x => x.post_category_type_id,
                        principalTable: "post_category_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_categories_post_types_post_type_id",
                        column: x => x.post_type_id,
                        principalTable: "post_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_category_type_meta_fields",
                columns: table => new
                {
                    post_category_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_field_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_category_type_meta_fields", x => new { x.post_category_type_id, x.meta_field_id });
                    table.ForeignKey(
                        name: "fk_post_category_type_meta_fields_meta_fields_meta_field_id",
                        column: x => x.meta_field_id,
                        principalTable: "meta_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_category_type_meta_fields_post_category_types_post_cat",
                        column: x => x.post_category_type_id,
                        principalTable: "post_category_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_category_meta_values",
                columns: table => new
                {
                    post_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_category_meta_values", x => new { x.post_category_id, x.meta_value_id });
                    table.ForeignKey(
                        name: "fk_post_category_meta_values_meta_values_meta_value_id",
                        column: x => x.meta_value_id,
                        principalTable: "meta_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_category_meta_values_post_categories_post_category_id",
                        column: x => x.post_category_id,
                        principalTable: "post_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_post_categories_entity",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_post_categories_entity", x => new { x.post_id, x.post_category_id });
                    table.ForeignKey(
                        name: "fk_post_post_categories_entity_post_categories_post_category_id",
                        column: x => x.post_category_id,
                        principalTable: "post_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_post_categories_entity_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_categories_parent_id",
                table: "post_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_categories_post_category_type_id",
                table: "post_categories",
                column: "post_category_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_categories_post_type_id",
                table: "post_categories",
                column: "post_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_categories_root_id",
                table: "post_categories",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_category_meta_values_meta_value_id",
                table: "post_category_meta_values",
                column: "meta_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_category_type_meta_fields_meta_field_id",
                table: "post_category_type_meta_fields",
                column: "meta_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_post_categories_entity_post_category_id",
                table: "post_post_categories_entity",
                column: "post_category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_category_meta_values");

            migrationBuilder.DropTable(
                name: "post_category_type_meta_fields");

            migrationBuilder.DropTable(
                name: "post_post_categories_entity");

            migrationBuilder.DropTable(
                name: "post_categories");

            migrationBuilder.DropTable(
                name: "post_category_types");
        }
    }
}
