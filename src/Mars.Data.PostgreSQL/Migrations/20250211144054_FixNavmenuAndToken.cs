using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class FixNavmenuAndToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "user_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                comment: "Создан");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                table: "nav_menus",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                comment: "Создан",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "Создан");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "user_tokens");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                table: "nav_menus",
                type: "timestamp with time zone",
                nullable: false,
                comment: "Создан",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()",
                oldComment: "Создан");
        }
    }
}
