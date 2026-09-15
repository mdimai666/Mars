using FluentAssertions;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Integration.Tests;

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

    [Fact]
    public void GetDefaultConnectionString_EachDriver_HasNoStrayQuotes()
    {
        foreach (var driver in DatasourceConfig.DriverList)
        {
            var connectionString = new DatasourceConfig { Driver = driver }.GetDefaultConnectionString();

            connectionString.Should().NotBeNullOrWhiteSpace();
            connectionString.Should().NotStartWith("\"");
        }
    }
}
