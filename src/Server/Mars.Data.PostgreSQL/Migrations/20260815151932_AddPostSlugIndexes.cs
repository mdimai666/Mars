using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mars.Data.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddPostSlugIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Поиск поста по slug идёт через lower(slug) (GetDetailBySlug, ExistAsync) и lower(Slug)
            // в QueryLang-фронтах — обычный b-tree по slug не подошёл бы, нужен выражение-индекс.
            migrationBuilder.Sql("CREATE INDEX \"ix_posts_post_type_id_slug_lower\" ON \"posts\" (\"post_type_id\", lower(\"slug\"));");

            migrationBuilder.Sql("CREATE INDEX \"ix_posts_slug_lower\" ON \"posts\" (lower(\"slug\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"ix_posts_slug_lower\";");

            migrationBuilder.Sql("DROP INDEX \"ix_posts_post_type_id_slug_lower\";");
        }
    }
}
