using FluentAssertions;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Tests;

public class ViewDdlBuilderTests
{
    static ViewDdlResult Create(string? body, bool replace = false, SqlDialect dialect = SqlDialect.Postgres, string? schema = "public", string? name = "todo_done")
        => ViewDdlBuilder.Create(dialect, schema, name, body, replace);

    [Fact]
    public void Create_Postgres_GeneratesCreateViewWithQuotedSchema()
    {
        Create("SELECT * FROM todo").Sql.Should().Be("""
CREATE VIEW "public"."todo_done" AS
SELECT * FROM todo
""");
    }

    [Fact]
    public void Create_Replace_PostgresUsesOrReplace()
    {
        Create("SELECT 1", replace: true).Sql.Should().Be("""
CREATE OR REPLACE VIEW "public"."todo_done" AS
SELECT 1
""");
    }

    [Fact]
    public void Create_Replace_MsSqlUsesOrAlter()
    {
        var result = Create("SELECT 1", replace: true, dialect: SqlDialect.MsSql, schema: "dbo");

        result.Sql.Should().Be("""
CREATE OR ALTER VIEW [dbo].[todo_done] AS
SELECT 1
""");
    }

    [Fact]
    public void Create_MsSqlWithoutReplace_GeneratesPlainCreateView()
    {
        var result = Create("SELECT 1", dialect: SqlDialect.MsSql, schema: "dbo");

        result.Sql.Should().Be("""
CREATE VIEW [dbo].[todo_done] AS
SELECT 1
""");
    }

    [Fact]
    public void Create_MySql_QuotesWithBackticks()
    {
        var result = Create("SELECT 1", replace: true, dialect: SqlDialect.MySql, schema: "shop");

        result.Sql.Should().Be("""
CREATE OR REPLACE VIEW `shop`.`todo_done` AS
SELECT 1
""");
    }

    [Fact]
    public void Create_NoSchema_UsesTableNameOnly()
    {
        Create("SELECT 1", schema: null).Sql.Should().Be("""
CREATE VIEW "todo_done" AS
SELECT 1
""");
    }

    [Fact]
    public void Create_MsSqlNameWithBracket_EscapesBracket()
    {
        var result = Create("SELECT 1", dialect: SqlDialect.MsSql, schema: null, name: "we]ird");

        result.Sql.Should().Be("""
CREATE VIEW [we]]ird] AS
SELECT 1
""");
    }

    [Fact]
    public void Create_TrailingSemicolon_DroppedFromBody()
    {
        Create("SELECT 1;\n").Sql.Should().Be("""
CREATE VIEW "public"."todo_done" AS
SELECT 1
""");
    }

    [Fact]
    public void Create_TrailingSemicolonFollowedByComment_KeepsCommentWithoutSemicolon()
    {
        Create("SELECT 1; -- keep").Sql.Should().Be("""
CREATE VIEW "public"."todo_done" AS
SELECT 1 -- keep
""");
    }

    [Fact]
    public void Create_LeadingCommentBeforeSelect_Accepted()
    {
        var result = Create("/* вьюха для отчёта */\nSELECT 1");

        result.Ok.Should().BeTrue();
        result.Sql.Should().Contain("SELECT 1");
    }

    [Fact]
    public void Create_SecondStatementInBody_RejectedWithPosition()
    {
        var result = Create("SELECT 1; DROP TABLE todo");

        result.Ok.Should().BeFalse();
        result.Error.Should().Contain("лишняя «;» на позиции 9");
    }

    [Theory]
    [InlineData("SELECT ';' AS a")]
    [InlineData("SELECT 1 AS a -- ; тут ничего нет")]
    [InlineData("SELECT 1 /* ; */ AS a")]
    [InlineData("SELECT $$a;b$$ AS a")]
    [InlineData("SELECT $tag$a;b$tag$ AS a")]
    [InlineData("SELECT \"col;name\" FROM t")]
    [InlineData("SELECT 'it''s; ok' AS a")]
    public void Create_SemicolonOutsideStatementSeparator_Accepted(string body)
    {
        Create(body).Ok.Should().BeTrue();
    }

    [Fact]
    public void Create_BackslashEscapedQuote_MySqlTreatsSemicolonAsInsideLiteral()
    {
        const string body = """SELECT 'a\'; DROP TABLE x'""";

        Create(body, dialect: SqlDialect.MySql).Ok.Should().BeTrue();
        Create(body, dialect: SqlDialect.Postgres).Error.Should().Contain("лишняя «;»");
    }

    [Theory]
    [InlineData("UPDATE todo SET a = 1", "UPDATE")]
    [InlineData("DROP TABLE todo", "DROP")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Create_NotASelect_Rejected(string? body, string? keyword)
    {
        var result = Create(body);

        result.Ok.Should().BeFalse();

        if (keyword is null)
        {
            result.Error.Should().Be("Тело вьюхи не задано");
        }
        else
        {
            result.Error.Should().Be($"Тело вьюхи — SELECT-запрос, а не «{keyword}»");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Rejected(string? name)
    {
        Create("SELECT 1", name: name).Error.Should().Be("Имя вьюхи не задано");
    }

    [Fact]
    public void Create_NameWithSchema_Rejected()
    {
        Create("SELECT 1", name: "public.todo_done").Error.Should().Be("Имя вьюхи — без схемы: схема выбирается отдельно");
    }

    [Theory]
    [InlineData(SqlDialect.Postgres, "public", "DROP VIEW \"public\".\"todo_done\"")]
    [InlineData(SqlDialect.MsSql, "dbo", "DROP VIEW [dbo].[todo_done]")]
    [InlineData(SqlDialect.MySql, "shop", "DROP VIEW `shop`.`todo_done`")]
    [InlineData(SqlDialect.Postgres, null, "DROP VIEW \"todo_done\"")]
    public void Drop_Dialect_GeneratesDropView(SqlDialect dialect, string? schema, string expected)
    {
        ViewDdlBuilder.Drop(dialect, schema, "todo_done").Sql.Should().Be(expected);
    }

    [Fact]
    public void Drop_EmptyName_Rejected()
    {
        ViewDdlBuilder.Drop(SqlDialect.Postgres, "public", null).Error.Should().Be("Имя вьюхи не задано");
    }
}
