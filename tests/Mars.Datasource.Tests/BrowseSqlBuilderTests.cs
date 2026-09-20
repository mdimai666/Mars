using FluentAssertions;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Tests;

public class BrowseSqlBuilderTests
{
    [Fact]
    public void Build_Postgres_OrdersByKeyAndLimits()
    {
        var sql = BrowseSqlBuilder.Build(SqlDialect.Postgres, "public", "todo", ["id"], 50);

        sql.Should().Be("SELECT * FROM \"public\".\"todo\" ORDER BY \"id\" LIMIT 50");
    }

    [Fact]
    public void Build_MsSql_PutsLimitBeforeColumns()
    {
        var sql = BrowseSqlBuilder.Build(SqlDialect.MsSql, "dbo", "todo", ["Id"], 50);

        sql.Should().Be("SELECT TOP 50 * FROM [dbo].[todo] ORDER BY [Id]");
    }

    [Fact]
    public void Build_MySql_QuotesWithBackticks()
    {
        var sql = BrowseSqlBuilder.Build(SqlDialect.MySql, "shop", "todo", ["Id"], 50);

        sql.Should().Be("SELECT * FROM `shop`.`todo` ORDER BY `Id` LIMIT 50");
    }

    [Fact]
    public void Build_NoKeyColumns_OmitsOrderBy()
    {
        // У вьюхи нет первичного ключа — сортировать не по чему, это нормальный случай.
        BrowseSqlBuilder.Build(SqlDialect.Postgres, "public", "todo_view", [], 50)
            .Should().Be("SELECT * FROM \"public\".\"todo_view\" LIMIT 50");
    }

    [Fact]
    public void Build_CompositeKey_KeepsKeyOrder()
    {
        BrowseSqlBuilder.Build(SqlDialect.Postgres, "public", "todo", ["a", "b"], 50)
            .Should().Be("SELECT * FROM \"public\".\"todo\" ORDER BY \"a\", \"b\" LIMIT 50");
    }

    [Fact]
    public void Build_NoSchema_UsesTableNameOnly()
    {
        BrowseSqlBuilder.Build(SqlDialect.Postgres, null, "todo", [], 50)
            .Should().Be("SELECT * FROM \"todo\" LIMIT 50");
    }

    [Theory]
    [InlineData(SqlDialect.Postgres, null, null, "SELECT * FROM \"todo\"")]
    [InlineData(SqlDialect.MsSql, "dbo", "Id", "SELECT * FROM [dbo].[todo] ORDER BY [Id]")]
    public void Build_ZeroLimit_LeavesOnlyServerSideCap(SqlDialect dialect, string? schema, string? key, string expected)
    {
        BrowseSqlBuilder.Build(dialect, schema, "todo", key is null ? [] : [key], 0).Should().Be(expected);
    }

    [Fact]
    public void Count_QuotesTarget()
    {
        BrowseSqlBuilder.Count(SqlDialect.Postgres, "public", "todo").Should().Be("SELECT COUNT(*) FROM \"public\".\"todo\"");
        BrowseSqlBuilder.Count(SqlDialect.MsSql, "dbo", "todo").Should().Be("SELECT COUNT(*) FROM [dbo].[todo]");
    }
}
