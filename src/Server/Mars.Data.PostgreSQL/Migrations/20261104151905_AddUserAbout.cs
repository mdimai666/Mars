using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAbout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "about",
                table: "users",
                type: "text",
                nullable: true,
                comment: "О себе");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "about",
                table: "users");
        }
    }
}
