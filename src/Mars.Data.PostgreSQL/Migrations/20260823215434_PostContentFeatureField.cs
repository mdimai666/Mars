using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations {
    /// <inheritdoc />
    public partial class PostContentFeatureField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Ключи редакторов первой волны — в схему core.input.*
            migrationBuilder.Sql("""
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"core.input.color"') WHERE options->>'editor' = 'color';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"core.input.url"') WHERE options->>'editor' = 'url';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"core.input.email"') WHERE options->>'editor' = 'email';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"core.input.date"') WHERE options->>'editor' = 'date';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"core.input.time"') WHERE options->>'editor' = 'time';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"core.input.datetime"') WHERE options->>'editor' = 'datetime';
                """);

            // 2. Поле контента фичи «Контент»: создаётся (или используется существующее
            //    с ключом 'content'), редактор и язык кода переносятся из пост-контентных
            //    настроек типа — ДО сноса колонки.
            migrationBuilder.Sql("""
                INSERT INTO meta_fields (id, created_at, modified_at, title, key, type, description, is_nullable, options, "order", tags, hidden, disabled, post_type_id)
                SELECT gen_random_uuid(), now(), NULL, 'Контент', 'content', 28, '', true,
                       jsonb_build_object('featureKey', 'content')
                       || CASE pt.post_content_type->>'PostContentType'
                              WHEN 'WYSIWYG' THEN '{"editor":"core.wysiwyg.quilljs"}'::jsonb
                              WHEN 'Code' THEN jsonb_build_object('editor', 'core.code.monaco', 'codeLang', coalesce(pt.post_content_type->>'CodeLang', 'handlebars'))
                              WHEN 'BlockEditor' THEN '{"editor":"core.blockeditor.editorjs"}'::jsonb
                              ELSE '{}'::jsonb END,
                       COALESCE((SELECT max(mf2."order") + 1 FROM meta_fields mf2 WHERE mf2.post_type_id = pt.id), 0),
                       '{}', false, false, pt.id
                FROM post_types pt
                WHERE pt.enabled_features @> '["Content"]'::jsonb
                  AND NOT EXISTS (SELECT 1 FROM meta_fields mf WHERE mf.post_type_id = pt.id AND mf.key = 'content');

                UPDATE meta_fields mf
                SET options = COALESCE(mf.options, '{}'::jsonb)
                    || '{"featureKey":"content"}'::jsonb
                    || CASE pt.post_content_type->>'PostContentType'
                           WHEN 'WYSIWYG' THEN '{"editor":"core.wysiwyg.quilljs"}'::jsonb
                           WHEN 'Code' THEN jsonb_build_object('editor', 'core.code.monaco', 'codeLang', coalesce(pt.post_content_type->>'CodeLang', 'handlebars'))
                           WHEN 'BlockEditor' THEN '{"editor":"core.blockeditor.editorjs"}'::jsonb
                           ELSE '{}'::jsonb END
                FROM post_types pt
                WHERE mf.post_type_id = pt.id
                  AND mf.key = 'content'
                  AND pt.enabled_features @> '["Content"]'::jsonb;
                """);

            migrationBuilder.AddColumn<JsonNode>(
                name: "options",
                table: "post_types",
                type: "jsonb",
                nullable: true,
                comment: "Опции (точка расширения)");

            migrationBuilder.DropColumn(
                name: "post_content_type",
                table: "post_types");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "post_content_type",
                table: "post_types",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            // настройки контента возвращаются из поля контента
            migrationBuilder.Sql("""
                UPDATE post_types pt
                SET post_content_type = jsonb_strip_nulls(jsonb_build_object(
                        'PostContentType', CASE mf.options->>'editor'
                            WHEN 'core.wysiwyg.quilljs' THEN 'WYSIWYG'
                            WHEN 'core.code.monaco' THEN 'Code'
                            WHEN 'core.blockeditor.editorjs' THEN 'BlockEditor'
                            ELSE 'PlainText' END,
                        'CodeLang', mf.options->>'codeLang'))
                FROM meta_fields mf
                WHERE mf.post_type_id = pt.id
                  AND mf.key = 'content'
                  AND pt.enabled_features @> '["Content"]'::jsonb;
                """);

            migrationBuilder.DropColumn(
                name: "options",
                table: "post_types");

            // ключи редакторов первой волны — обратно
            migrationBuilder.Sql("""
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"color"') WHERE options->>'editor' = 'core.input.color';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"url"') WHERE options->>'editor' = 'core.input.url';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"email"') WHERE options->>'editor' = 'core.input.email';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"date"') WHERE options->>'editor' = 'core.input.date';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"time"') WHERE options->>'editor' = 'core.input.time';
                UPDATE meta_fields SET options = jsonb_set(options, '{editor}', '"datetime"') WHERE options->>'editor' = 'core.input.datetime';

                DELETE FROM meta_fields mf
                USING post_types pt
                WHERE mf.post_type_id = pt.id
                  AND mf.key = 'content'
                  AND mf.options->>'featureKey' = 'content';
                """);
        }
    }
}
