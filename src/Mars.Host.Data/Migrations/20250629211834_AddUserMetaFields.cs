using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMetaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_meta_fields");

            migrationBuilder.CreateTable(
                name: "user_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    title = table.Column<string>(type: "text", maxLength: 256, nullable: false, comment: "Название"),
                    type_name = table.Column<string>(type: "varchar(128)", maxLength: 100, nullable: false, comment: "Тип"),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, comment: "Теги")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_types", x => x.id);
                });

            var defaultUserTypeId = Guid.NewGuid();
            migrationBuilder.Sql($@"
                INSERT INTO user_types (id, created_at, title, type_name, tags)
                VALUES (
                    '{defaultUserTypeId}',
                    now(),
                    'default',
                    'default',
                    ARRAY[]::varchar(128)[]
                );
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "user_type_id",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql($@"
                UPDATE users
                SET user_type_id = '{defaultUserTypeId}';
            ");

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
                name: "ix_users_user_type_id",
                table: "users",
                column: "user_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_type_meta_fields_meta_field_id",
                table: "user_type_meta_fields",
                column: "meta_field_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_user_types_user_type_id",
                table: "users",
                column: "user_type_id",
                principalTable: "user_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_user_types_user_type_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "user_type_meta_fields");

            migrationBuilder.DropTable(
                name: "user_types");

            migrationBuilder.DropIndex(
                name: "ix_users_user_type_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_type_id",
                table: "users");

            migrationBuilder.CreateTable(
                name: "user_meta_fields",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_field_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_meta_fields", x => new { x.user_id, x.meta_field_id });
                    table.ForeignKey(
                        name: "fk_user_meta_fields_meta_fields_meta_field_id",
                        column: x => x.meta_field_id,
                        principalTable: "meta_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_meta_fields_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_meta_fields_meta_field_id",
                table: "user_meta_fields",
                column: "meta_field_id");
        }
    }
}
