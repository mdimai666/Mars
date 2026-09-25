using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Tests;

public class DatasourceConfigTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("a")]
    [InlineData("default")]
    [InlineData("DEFAULT")]
    [InlineData("MySource")]
    [InlineData("my source")]
    [InlineData("источник")]
    [InlineData("source!")]
    public void ValidateSlug_InvalidSlug_ReturnsError(string? slug)
    {
        DatasourceConfig.ValidateSlug(slug).Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("appdb")]
    [InlineData("my-source_2")]
    [InlineData("a1")]
    [InlineData("test_psql")]
    public void ValidateSlug_ValidSlug_ReturnsNull(string slug)
    {
        DatasourceConfig.ValidateSlug(slug).Should().BeNull();
    }

    [Fact]
    public void GetDatabaseName_EmptyConnectionString_ReturnsEmpty()
    {
        // Раньше падало исключением: ToDictionary на пустой строке ломало редактирование конфигурации.
        new DatasourceConfig().GetDatabaseName().Should().BeEmpty();
        new DatasourceConfig().IsDefaultString().Should().BeFalse();
    }

    [Fact]
    public void GetDatabaseName_MySqlStyleConnectionString_ReturnsDatabase()
    {
        var config = new DatasourceConfig { ConnectionString = "server=127.0.0.1;database=wordpress;uid=wp;pwd=wp;" };

        config.GetDatabaseName().Should().Be("wordpress");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_LegacyConfigWithoutKind_BecomesSqlWithDriver(string? kind)
    {
        var config = new DatasourceConfig { Kind = kind!, Driver = "mysql" };

        config.Normalize();

        config.Kind.Should().Be(DatasourceKind.Sql);
        config.Driver.Should().Be("mysql");
    }

    [Fact]
    public void Normalize_SqlWithoutDriver_FallsBackToPsql()
    {
        var config = new DatasourceConfig { Kind = DatasourceKind.Sql, Driver = "" };

        config.Normalize();

        config.Driver.Should().Be("psql");
    }

    [Fact]
    public void Normalize_NonSqlKind_KeepsDriverUntouched()
    {
        var config = new DatasourceConfig { Kind = DatasourceKind.File, Driver = "" };

        config.Normalize();

        config.Kind.Should().Be(DatasourceKind.File);
        config.Driver.Should().BeEmpty();
    }
}
