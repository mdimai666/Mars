using System;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations {
    /// <inheritdoc />
    public partial class MetaFieldsOneToNOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_category_type_meta_fields");

            migrationBuilder.DropTable(
                name: "post_type_meta_fields");

            migrationBuilder.DropTable(
                name: "user_type_meta_fields");

            migrationBuilder.AddColumn<string>(
                name: "default",
                table: "meta_fields",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<JsonNode>(
                name: "options",
                table: "meta_fields",
                type: "jsonb",
                nullable: true,
                comment: "Опции (точка расширения)");

            migrationBuilder.AddColumn<Guid>(
                name: "post_category_type_id",
                table: "meta_fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "post_type_id",
                table: "meta_fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_type_id",
                table: "meta_fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_meta_fields_post_category_type_id_key",
                table: "meta_fields",
                columns: new[] { "post_category_type_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meta_fields_post_type_id_key",
                table: "meta_fields",
                columns: new[] { "post_type_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meta_fields_user_type_id_key",
                table: "meta_fields",
                columns: new[] { "user_type_id", "key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_meta_fields_post_category_types_post_category_type_id",
                table: "meta_fields",
                column: "post_category_type_id",
                principalTable: "post_category_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_meta_fields_post_types_post_type_id",
                table: "meta_fields",
                column: "post_type_id",
                principalTable: "post_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_meta_fields_user_types_user_type_id",
                table: "meta_fields",
                column: "user_type_id",
                principalTable: "user_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_meta_fields_post_category_types_post_category_type_id",
                table: "meta_fields");

            migrationBuilder.DropForeignKey(
                name: "fk_meta_fields_post_types_post_type_id",
                table: "meta_fields");

            migrationBuilder.DropForeignKey(
                name: "fk_meta_fields_user_types_user_type_id",
                table: "meta_fields");

            migrationBuilder.DropIndex(
                name: "ix_meta_fields_post_category_type_id_key",
                table: "meta_fields");

            migrationBuilder.DropIndex(
                name: "ix_meta_fields_post_type_id_key",
                table: "meta_fields");

            migrationBuilder.DropIndex(
                name: "ix_meta_fields_user_type_id_key",
                table: "meta_fields");

            migrationBuilder.DropColumn(
                name: "default",
                table: "meta_fields");

            migrationBuilder.DropColumn(
                name: "options",
                table: "meta_fields");

            migrationBuilder.DropColumn(
                name: "post_category_type_id",
                table: "meta_fields");

            migrationBuilder.DropColumn(
                name: "post_type_id",
                table: "meta_fields");

            migrationBuilder.DropColumn(
                name: "user_type_id",
                table: "meta_fields");

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
                name: "post_type_meta_fields",
                columns: table => new
                {
                    post_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_field_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_type_meta_fields", x => new { x.post_type_id, x.meta_field_id });
                    table.ForeignKey(
                        name: "fk_post_type_meta_fields_meta_fields_meta_field_id",
                        column: x => x.meta_field_id,
                        principalTable: "meta_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_type_meta_fields_post_types_post_type_id",
                        column: x => x.post_type_id,
                        principalTable: "post_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_type_meta_fields",
                columns: table => new
                {
                    user_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_field_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_type_meta_fields", x => new { x.user_type_id, x.meta_field_id });
                    table.ForeignKey(
                        name: "fk_user_type_meta_fields_meta_fields_meta_field_id",
                        column: x => x.meta_field_id,
                        principalTable: "meta_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_type_meta_fields_user_types_user_type_id",
                        column: x => x.user_type_id,
                        principalTable: "user_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_category_type_meta_fields_meta_field_id",
                table: "post_category_type_meta_fields",
                column: "meta_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_type_meta_fields_meta_field_id",
                table: "post_type_meta_fields",
                column: "meta_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_type_meta_fields_meta_field_id",
                table: "user_type_meta_fields",
                column: "meta_field_id");
        }
    }
}
