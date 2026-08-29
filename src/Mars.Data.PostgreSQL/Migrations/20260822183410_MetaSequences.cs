using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class MetaSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meta_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "ИД"),
                    meta_field_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Мета-поле"),
                    scope_key = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "Скоуп счётчика (префикс, опционально + дата)"),
                    last_value = table.Column<long>(type: "bigint", nullable: false, comment: "Последний выданный номер"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meta_sequences", x => x.id);
                    table.ForeignKey(
                        name: "fk_meta_sequences_meta_fields_meta_field_id",
                        column: x => x.meta_field_id,
                        principalTable: "meta_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_meta_sequences_meta_field_id_scope_key",
                table: "meta_sequences",
                columns: new[] { "meta_field_id", "scope_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meta_sequences");
        }
    }
}
