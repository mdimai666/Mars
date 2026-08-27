using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Host.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasskeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_passkeys",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Создан"),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Изменен"),
                    disabled = table.Column<bool>(type: "boolean", nullable: false, comment: "Отключен"),
                    data_attestation_object = table.Column<byte[]>(type: "bytea", nullable: false),
                    data_client_data_json = table.Column<byte[]>(type: "bytea", nullable: false),
                    data_created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_is_backed_up = table.Column<bool>(type: "boolean", nullable: false),
                    data_is_backup_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    data_is_user_verified = table.Column<bool>(type: "boolean", nullable: false),
                    data_name = table.Column<string>(type: "text", nullable: true),
                    data_public_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    data_sign_count = table.Column<long>(type: "bigint", nullable: false),
                    data_transports = table.Column<string[]>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_passkeys", x => new { x.user_id, x.credential_id });
                    table.ForeignKey(
                        name: "fk_user_passkeys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_passkeys_credential_id",
                table: "user_passkeys",
                column: "credential_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_passkeys");
        }
    }
}
