using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class FamilyMetaValueTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_post_category_meta_values_meta_values_meta_value_id",
                table: "post_category_meta_values");

            migrationBuilder.DropForeignKey(
                name: "fk_post_meta_values_meta_values_meta_value_id",
                table: "post_meta_values");

            migrationBuilder.DropForeignKey(
                name: "fk_user_meta_values_meta_values_meta_value_id",
                table: "user_meta_values");

            migrationBuilder.DropTable(
                name: "meta_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_meta_values",
                table: "user_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_user_meta_values_meta_value_id",
                table: "user_meta_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_post_meta_values",
                table: "post_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_post_meta_values_meta_value_id",
                table: "post_meta_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_post_category_meta_values",
                table: "post_category_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_post_category_meta_values_meta_value_id",
                table: "post_category_meta_values");

            migrationBuilder.RenameColumn(
                name: "meta_value_id",
                table: "user_meta_values",
                newName: "meta_field_id");

            migrationBuilder.RenameColumn(
                name: "meta_value_id",
                table: "post_meta_values",
                newName: "meta_field_id");

            migrationBuilder.RenameColumn(
                name: "meta_value_id",
                table: "post_category_meta_values",
                newName: "meta_field_id");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "user_meta_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "ИД");

            migrationBuilder.AddColumn<bool>(
                name: "bool",
                table: "user_meta_values",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "user_meta_values",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                comment: "Создан");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_time",
                table: "user_meta_values",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "decimal",
                table: "user_meta_values",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "float",
                table: "user_meta_values",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "index",
                table: "user_meta_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "int",
                table: "user_meta_values",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "long",
                table: "user_meta_values",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "model_id",
                table: "user_meta_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "modified_at",
                table: "user_meta_values",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Изменен");

            migrationBuilder.AddColumn<string>(
                name: "string_short",
                table: "user_meta_values",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "string_text",
                table: "user_meta_values",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "user_meta_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "user_meta_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid[]>(
                name: "variants_ids",
                table: "user_meta_values",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "post_meta_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "ИД");

            migrationBuilder.AddColumn<bool>(
                name: "bool",
                table: "post_meta_values",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "post_meta_values",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                comment: "Создан");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_time",
                table: "post_meta_values",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "decimal",
                table: "post_meta_values",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "float",
                table: "post_meta_values",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "index",
                table: "post_meta_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "int",
                table: "post_meta_values",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "long",
                table: "post_meta_values",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "model_id",
                table: "post_meta_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "modified_at",
                table: "post_meta_values",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Изменен");

            migrationBuilder.AddColumn<string>(
                name: "string_short",
                table: "post_meta_values",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "string_text",
                table: "post_meta_values",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "post_meta_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "post_meta_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid[]>(
                name: "variants_ids",
                table: "post_meta_values",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "post_category_meta_values",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "ИД");

            migrationBuilder.AddColumn<bool>(
                name: "bool",
                table: "post_category_meta_values",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "post_category_meta_values",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                comment: "Создан");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_time",
                table: "post_category_meta_values",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "decimal",
                table: "post_category_meta_values",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "float",
                table: "post_category_meta_values",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "index",
                table: "post_category_meta_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "int",
                table: "post_category_meta_values",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "long",
                table: "post_category_meta_values",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "model_id",
                table: "post_category_meta_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "modified_at",
                table: "post_category_meta_values",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Изменен");

            migrationBuilder.AddColumn<string>(
                name: "string_short",
                table: "post_category_meta_values",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "string_text",
                table: "post_category_meta_values",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "post_category_meta_values",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "variant_id",
                table: "post_category_meta_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid[]>(
                name: "variants_ids",
                table: "post_category_meta_values",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_meta_values",
                table: "user_meta_values",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_post_meta_values",
                table: "post_meta_values",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_post_category_meta_values",
                table: "post_category_meta_values",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_user_meta_values_meta_field_id_string_short",
                table: "user_meta_values",
                columns: new[] { "meta_field_id", "string_short" });

            migrationBuilder.CreateIndex(
                name: "ix_user_meta_values_user_id",
                table: "user_meta_values",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_meta_values_meta_field_id_string_short",
                table: "post_meta_values",
                columns: new[] { "meta_field_id", "string_short" });

            migrationBuilder.CreateIndex(
                name: "ix_post_meta_values_post_id",
                table: "post_meta_values",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_category_meta_values_meta_field_id_string_short",
                table: "post_category_meta_values",
                columns: new[] { "meta_field_id", "string_short" });

            migrationBuilder.CreateIndex(
                name: "ix_post_category_meta_values_post_category_id",
                table: "post_category_meta_values",
                column: "post_category_id");

            migrationBuilder.AddForeignKey(
                name: "fk_post_category_meta_values_meta_fields_meta_field_id",
                table: "post_category_meta_values",
                column: "meta_field_id",
                principalTable: "meta_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_post_meta_values_meta_fields_meta_field_id",
                table: "post_meta_values",
                column: "meta_field_id",
                principalTable: "meta_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_meta_values_meta_fields_meta_field_id",
                table: "user_meta_values",
                column: "meta_field_id",
                principalTable: "meta_fields",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_post_category_meta_values_meta_fields_meta_field_id",
                table: "post_category_meta_values");

            migrationBuilder.DropForeignKey(
                name: "fk_post_meta_values_meta_fields_meta_field_id",
                table: "post_meta_values");

            migrationBuilder.DropForeignKey(
                name: "fk_user_meta_values_meta_fields_meta_field_id",
                table: "user_meta_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_meta_values",
                table: "user_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_user_meta_values_meta_field_id_string_short",
                table: "user_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_user_meta_values_user_id",
                table: "user_meta_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_post_meta_values",
                table: "post_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_post_meta_values_meta_field_id_string_short",
                table: "post_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_post_meta_values_post_id",
                table: "post_meta_values");

            migrationBuilder.DropPrimaryKey(
                name: "pk_post_category_meta_values",
                table: "post_category_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_post_category_meta_values_meta_field_id_string_short",
                table: "post_category_meta_values");

            migrationBuilder.DropIndex(
                name: "ix_post_category_meta_values_post_category_id",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "id",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "bool",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "date_time",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "decimal",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "float",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "index",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "int",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "long",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "model_id",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "modified_at",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "string_short",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "string_text",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "type",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "variants_ids",
                table: "user_meta_values");

            migrationBuilder.DropColumn(
                name: "id",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "bool",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "date_time",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "decimal",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "float",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "index",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "int",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "long",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "model_id",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "modified_at",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "string_short",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "string_text",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "type",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "variants_ids",
                table: "post_meta_values");

            migrationBuilder.DropColumn(
                name: "id",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "bool",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "date_time",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "decimal",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "float",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "index",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "int",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "long",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "model_id",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "modified_at",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "string_short",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "string_text",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "type",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "post_category_meta_values");

            migrationBuilder.DropColumn(
                name: "variants_ids",
                table: "post_category_meta_values");

            migrationBuilder.RenameColumn(
                name: "meta_field_id",
                table: "user_meta_values",
                newName: "meta_value_id");

            migrationBuilder.RenameColumn(
                name: "meta_field_id",
                table: "post_meta_values",
                newName: "meta_value_id");

            migrationBuilder.RenameColumn(
                name: "meta_field_id",
                table: "post_category_meta_values",
                newName: "meta_value_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_meta_values",
                table: "user_meta_values",
                columns: new[] { "user_id", "meta_value_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_post_meta_values",
                table: "post_meta_values",
                columns: new[] { "post_id", "meta_value_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_post_category_meta_values",
                table: "post_category_meta_values",
                columns: new[] { "post_category_id", "meta_value_id" });

            migrationBuilder.CreateTable(
                name: "meta_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    meta_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @bool = table.Column<bool>(name: "bool", type: "boolean", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    @decimal = table.Column<decimal>(name: "decimal", type: "numeric", nullable: true),
                    @float = table.Column<double>(name: "float", type: "double precision", nullable: true),
                    index = table.Column<int>(type: "integer", nullable: false),
                    @int = table.Column<int>(name: "int", type: "integer", nullable: true),
                    @long = table.Column<long>(name: "long", type: "bigint", nullable: true),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    string_short = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    string_text = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    variants_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meta_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_meta_values_meta_fields_meta_field_id",
                        column: x => x.meta_field_id,
                        principalTable: "meta_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_meta_values_meta_value_id",
                table: "user_meta_values",
                column: "meta_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_meta_values_meta_value_id",
                table: "post_meta_values",
                column: "meta_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_category_meta_values_meta_value_id",
                table: "post_category_meta_values",
                column: "meta_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_meta_values_meta_field_id",
                table: "meta_values",
                column: "meta_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_meta_values_meta_field_id_string_short",
                table: "meta_values",
                columns: new[] { "meta_field_id", "string_short" });

            migrationBuilder.AddForeignKey(
                name: "fk_post_category_meta_values_meta_values_meta_value_id",
                table: "post_category_meta_values",
                column: "meta_value_id",
                principalTable: "meta_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_post_meta_values_meta_values_meta_value_id",
                table: "post_meta_values",
                column: "meta_value_id",
                principalTable: "meta_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_meta_values_meta_values_meta_value_id",
                table: "user_meta_values",
                column: "meta_value_id",
                principalTable: "meta_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
