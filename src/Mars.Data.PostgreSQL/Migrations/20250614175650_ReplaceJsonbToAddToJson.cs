using System.Collections.Generic;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Data.OwnedTypes.NavMenus;
using Mars.Host.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceJsonbToAddToJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "post_status_list",
                table: "post_types",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(List<PostStatusEntity>),
                oldType: "jsonb",
                oldComment: "РЎС‚Р°С‚СѓСЃС‹");

            migrationBuilder.AlterColumn<string>(
                name: "post_content_type",
                table: "post_types",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldComment: "РќР°СЃС‚СЂРѕР№РєРё РєРѕРЅС‚РµРЅС‚Р°");

            migrationBuilder.AlterColumn<string>(
                name: "menu_items",
                table: "nav_menus",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(List<NavMenuItem>),
                oldType: "jsonb",
                oldComment: "Р­Р»РµРјРµРЅС‚С‹");

            migrationBuilder.AlterColumn<string>(
                name: "variants",
                table: "meta_fields",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(List<MetaFieldVariant>),
                oldType: "jsonb",
                oldComment: "Р’Р°СЂРёР°РЅС‚С‹");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<PostStatusEntity>>(
                name: "post_status_list",
                table: "post_types",
                type: "jsonb",
                nullable: false,
                comment: "РЎС‚Р°С‚СѓСЃС‹",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "post_content_type",
                table: "post_types",
                type: "jsonb",
                nullable: false,
                comment: "РќР°СЃС‚СЂРѕР№РєРё РєРѕРЅС‚РµРЅС‚Р°",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<List<NavMenuItem>>(
                name: "menu_items",
                table: "nav_menus",
                type: "jsonb",
                nullable: false,
                comment: "Р­Р»РµРјРµРЅС‚С‹",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<MetaFieldVariant>>(
                name: "variants",
                table: "meta_fields",
                type: "jsonb",
                nullable: false,
                comment: "Р’Р°СЂРёР°РЅС‚С‹",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
