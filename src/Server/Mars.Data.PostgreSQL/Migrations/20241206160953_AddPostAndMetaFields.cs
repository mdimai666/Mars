using Mars.Data.Entities;
using Mars.Data.OwnedTypes.MetaFields;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddPostAndMetaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meta_fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "РР”"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "РЎРѕР·РґР°РЅ"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "РР·РјРµРЅРµРЅ"),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Р РѕРґРёС‚РµР»СЊСЃРєРѕРµ РјРµС‚Р° РїРѕР»Рµ"),
                    title = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, comment: "РќР°Р·РІР°РЅРёРµ"),
                    key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, comment: "Key"),
                    type = table.Column<int>(type: "integer", nullable: false, comment: "РўРёРї"),
                    variants = table.Column<List<MetaFieldVariant>>(type: "jsonb", nullable: false, comment: "Р’Р°СЂРёР°РЅС‚С‹"),
                    max_value = table.Column<decimal>(type: "numeric", nullable: true, comment: "РњР°РєСЃРёРјР°Р»СЊРЅРѕРµ"),
                    min_value = table.Column<decimal>(type: "numeric", nullable: true, comment: "РњРёРЅРёРјР°Р»СЊРЅРѕРµ"),
                    description = table.Column<string>(type: "text", maxLength: 256, nullable: false, comment: "РћРїРёСЃР°РЅРёРµ"),
                    is_nullable = table.Column<bool>(type: "boolean", nullable: false, comment: "IsNullable"),
                    order = table.Column<int>(type: "integer", nullable: false, comment: "РџРѕСЂСЏРґРѕРє"),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, comment: "РўРµРіРё"),
                    hidden = table.Column<bool>(type: "boolean", nullable: false, comment: "РЎРєСЂС‹С‚РѕРµ"),
                    disabled = table.Column<bool>(type: "boolean", nullable: false, comment: "РћС‚РєР»СЋС‡РµРЅ"),
                    model_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, comment: "РњРѕРґРµР»СЊ")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meta_fields", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "РР”"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "РЎРѕР·РґР°РЅ"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "РР·РјРµРЅРµРЅ"),
                    title = table.Column<string>(type: "text", maxLength: 256, nullable: false, comment: "РќР°Р·РІР°РЅРёРµ"),
                    type_name = table.Column<string>(type: "varchar(128)", maxLength: 100, nullable: false, comment: "РўРёРї"),
                    post_status_list = table.Column<List<PostStatusEntity>>(type: "jsonb", nullable: false, comment: "РЎС‚Р°С‚СѓСЃС‹"),
                    enabled_features = table.Column<List<string>>(type: "jsonb", nullable: false, comment: "Р¤СѓРЅРєС†РёРё"),
                    disabled = table.Column<bool>(type: "boolean", nullable: false, comment: "РћС‚РєР»СЋС‡РµРЅ"),
                    post_content_type = table.Column<string>(type: "jsonb", nullable: false, comment: "РќР°СЃС‚СЂРѕР№РєРё РєРѕРЅС‚РµРЅС‚Р°"),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, comment: "РўРµРіРё")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_types", x => x.id);
                    table.UniqueConstraint("ak_post_types_type_name", x => x.type_name);
                });

            migrationBuilder.CreateTable(
                name: "meta_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "РР”"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "РЎРѕР·РґР°РЅ"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "РР·РјРµРЅРµРЅ"),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    @bool = table.Column<bool>(name: "bool", type: "boolean", nullable: false),
                    @int = table.Column<int>(name: "int", type: "integer", nullable: false),
                    @float = table.Column<float>(name: "float", type: "real", nullable: false),
                    @decimal = table.Column<decimal>(name: "decimal", type: "numeric", nullable: false),
                    @long = table.Column<long>(name: "long", type: "bigint", nullable: false),
                    string_text = table.Column<string>(type: "text", nullable: true),
                    string_short = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    @null = table.Column<bool>(name: "null", type: "boolean", nullable: false),
                    date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variants_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_field_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "РР”"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "РЎРѕР·РґР°РЅ"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "РР·РјРµРЅРµРЅ"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "РЈРґР°Р»РµРЅ РІ"),
                    slug = table.Column<string>(type: "varchar(256)", nullable: false, comment: "slug"),
                    tags = table.Column<List<string>>(type: "character varying(128)[]", nullable: false, comment: "РўРµРіРё"),
                    title = table.Column<string>(type: "text", maxLength: 512, nullable: false, comment: "РќР°Р·РІР°РЅРёРµ"),
                    content = table.Column<string>(type: "text", nullable: true, comment: "РўРµРєСЃС‚"),
                    excerpt = table.Column<string>(type: "text", maxLength: 512, nullable: true, comment: "РћС‚СЂС‹РІРѕРє"),
                    status = table.Column<string>(type: "varchar(256)", nullable: false, comment: "РЎС‚Р°С‚СѓСЃ"),
                    type = table.Column<string>(type: "varchar(128)", nullable: false, comment: "РўРёРї"),
                    lang_code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, comment: "РЇР·С‹Рє")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                    table.ForeignKey(
                        name: "fk_posts_post_types_type",
                        column: x => x.type,
                        principalTable: "post_types",
                        principalColumn: "type_name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_meta_values",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_meta_values", x => new { x.user_id, x.meta_value_id });
                    table.ForeignKey(
                        name: "fk_user_meta_values_meta_values_meta_value_id",
                        column: x => x.meta_value_id,
                        principalTable: "meta_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_meta_values_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_files_entity",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_entity_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_files_entity", x => new { x.post_id, x.file_entity_id });
                    table.ForeignKey(
                        name: "fk_post_files_entity_files_file_entity_id",
                        column: x => x.file_entity_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_files_entity_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_meta_values",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_meta_values", x => new { x.post_id, x.meta_value_id });
                    table.ForeignKey(
                        name: "fk_post_meta_values_meta_values_meta_value_id",
                        column: x => x.meta_value_id,
                        principalTable: "meta_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_meta_values_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_meta_values_meta_field_id",
                table: "meta_values",
                column: "meta_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_files_entity_file_entity_id",
                table: "post_files_entity",
                column: "file_entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_meta_values_meta_value_id",
                table: "post_meta_values",
                column: "meta_value_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_type_meta_fields_meta_field_id",
                table: "post_type_meta_fields",
                column: "meta_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_types_type_name",
                table: "post_types",
                column: "type_name",
                filter: "\"disabled\" IS true");

            migrationBuilder.CreateIndex(
                name: "ix_posts_type",
                table: "posts",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_user_meta_fields_meta_field_id",
                table: "user_meta_fields",
                column: "meta_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_meta_values_meta_value_id",
                table: "user_meta_values",
                column: "meta_value_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_files_entity");

            migrationBuilder.DropTable(
                name: "post_meta_values");

            migrationBuilder.DropTable(
                name: "post_type_meta_fields");

            migrationBuilder.DropTable(
                name: "user_meta_fields");

            migrationBuilder.DropTable(
                name: "user_meta_values");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "meta_values");

            migrationBuilder.DropTable(
                name: "post_types");

            migrationBuilder.DropTable(
                name: "meta_fields");
        }
    }
}
