using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "ИД пользователя");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "files",
                type: "uuid",
                nullable: false,
                comment: "ИД пользователя",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_posts_user_id",
                table: "posts",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_posts_users_user_id",
                table: "posts",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_posts_users_user_id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_posts_user_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "posts");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "files",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "ИД пользователя");
        }
    }
}
