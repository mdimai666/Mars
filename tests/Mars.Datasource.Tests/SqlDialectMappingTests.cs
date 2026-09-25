using FluentAssertions;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Tests;

public class SqlDialectMappingTests
{
    [Theory]
    [InlineData("psql", SqlDialect.Postgres)]
    [InlineData("mssql", SqlDialect.MsSql)]
    [InlineData("mysql", SqlDialect.MySql)]
    [InlineData("MSSQL", SqlDialect.MsSql)]
    [InlineData("  psql ", SqlDialect.Postgres)]
    [InlineData("", SqlDialect.Postgres)]
    [InlineData(null, SqlDialect.Postgres)]
    public void Dialect_DriverName_ReturnsDialect(string? driver, SqlDialect expected)
    {
        SqlDialectMapping.Dialect(driver).Should().Be(expected);
    }

    [Theory]
    [InlineData(SqlDialect.Postgres, "a\"b", "\"a\"\"b\"")]
    [InlineData(SqlDialect.MsSql, "a]b", "[a]]b]")]
    [InlineData(SqlDialect.MySql, "a`b", "`a``b`")]
    [InlineData(SqlDialect.Postgres, "todo", "\"todo\"")]
    public void Quote_NameWithQuoteChar_EscapesPerDialect(SqlDialect dialect, string name, string expected)
    {
        SqlDialectMapping.Quote(dialect, name).Should().Be(expected);
    }

    [Theory]
    [InlineData("public", "\"public\".\"todo\"")]
    [InlineData(null, "\"todo\"")]
    [InlineData("", "\"todo\"")]
    [InlineData("   ", "\"todo\"")]
    public void Target_SchemaName_QualifiesOnlyWhenSet(string? schema, string expected)
    {
        SqlDialectMapping.Target(SqlDialect.Postgres, schema, "todo").Should().Be(expected);
    }

    [Fact]
    public void Target_MsSql_QuotesSchemaAndTable()
    {
        SqlDialectMapping.Target(SqlDialect.MsSql, "dbo", "todo").Should().Be("[dbo].[todo]");
    }
}
