using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdPostTypeToPostById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_posts_post_types_type",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_posts_type",
                table: "posts");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_post_types_type_name",
                table: "post_types");

            migrationBuilder.DropColumn(
                name: "type",
                table: "posts");

            migrationBuilder.AddColumn<Guid>(
                name: "post_type_id",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_posts_post_type_id",
                table: "posts",
                column: "post_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_posts_post_types_post_type_id",
                table: "posts",
                column: "post_type_id",
                principalTable: "post_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_posts_post_types_post_type_id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_posts_post_type_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "post_type_id",
                table: "posts");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "posts",
                type: "varchar(128)",
                nullable: false,
                defaultValue: "",
                comment: "Тип");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_post_types_type_name",
                table: "post_types",
                column: "type_name");

            migrationBuilder.CreateIndex(
                name: "ix_posts_type",
                table: "posts",
                column: "type");

            migrationBuilder.AddForeignKey(
                name: "fk_posts_post_types_type",
                table: "posts",
                column: "type",
                principalTable: "post_types",
                principalColumn: "type_name",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
