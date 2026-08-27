using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations {
    /// <inheritdoc />
    public partial class MetaFieldIsMultiple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_multiple",
                table: "meta_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Множественное: поле допускает несколько значений");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_multiple",
                table: "meta_fields");
        }
    }
}
