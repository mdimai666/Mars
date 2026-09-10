using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class PostTypeImageFieldKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_field_key",
                table: "post_types",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Ключ мета-поля — картинки типа (указатель превью)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_field_key",
                table: "post_types");
        }
    }
}
