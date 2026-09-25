using FluentAssertions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Providers.MsSQL;
using Mars.Datasource.Providers.MySQL;
using Mars.Datasource.Providers.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Tests;

/// <summary>
/// Провайдеры источников: каждый регистрируется хуком своего проекта, а метаданные для формы
/// настроек (подсказка строки подключения и ссылка на док) живут у провайдера, не в ядре.
/// </summary>
public class DatasourceProviderTests
{
    static ServiceProvider Providers()
        => new ServiceCollection()
            .AddDatasourcePostgreSql()
            .AddDatasourceMsSql()
            .AddDatasourceMySql()
            .BuildServiceProvider();

    [Theory]
    [InlineData("psql")]
    [InlineData("mssql")]
    [InlineData("mysql")]
    public void DriverFactory_HasConnectionHintAndHelpLink(string driver)
    {
        var factory = Providers().GetServices<IDatasourceDriverFactory>().SingleOrDefault(f => f.Driver == driver);

        factory.Should().NotBeNull();
        factory!.DefaultConnectionString.Should().NotBeNullOrWhiteSpace().And.NotStartWith("\"");
        factory.HelpLink.Should().NotBeNullOrWhiteSpace();
        factory.Create(new DatasourceConfig { Driver = driver }).Should().NotBeNull();
    }

    [Fact]
    public void DriverFactories_RegisteredOncePerDriver()
    {
        var drivers = Providers().GetServices<IDatasourceDriverFactory>().Select(f => f.Driver).ToList();

        drivers.Should().BeEquivalentTo(["psql", "mssql", "mysql"]);
    }

    [Fact]
    public void BackupDriver_PostgreSqlOnly()
    {
        var drivers = Providers().GetServices<IDatasourceBackupDriver>().Select(d => d.Driver).ToList();

        drivers.Should().BeEquivalentTo(["psql"]);
    }
}
