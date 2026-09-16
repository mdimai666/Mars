using FluentAssertions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Contracts.Dto;
using Mars.Datasource.Contracts.Models;
using Mars.Datasource.Host.Services;
using Mars.Datasource.Providers.MsSQL;
using Mars.Datasource.Providers.MySQL;
using Mars.Datasource.Providers.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Integration.Tests;

/// <summary>
/// Реестр провайдеров: sql-движки приходят фабриками драйверов и оборачиваются,
/// остальные источники регистрируют свои фабрики. Резолв — по типу источника и драйверу.
/// </summary>
public class DatasourceProviderRegistryTests
{
    static IDatasourceProviderRegistry Registry(params IDatasourceProviderFactory[] providers)
        => new DatasourceProviderRegistry(SqlDriverFactories(), providers);

    static IEnumerable<IDatasourceDriverFactory> SqlDriverFactories()
        => new ServiceCollection()
            .AddDatasourcePostgreSql()
            .AddDatasourceMsSql()
            .AddDatasourceMySql()
            .BuildServiceProvider()
            .GetServices<IDatasourceDriverFactory>();

    [Theory]
    [InlineData("psql", typeof(DatasourcePostgreSQLDriver))]
    [InlineData("mssql", typeof(DatasourceMsSQLDriver))]
    [InlineData("mysql", typeof(DatasourceMySQLDriver))]
    public void Resolve_SqlDriver_ReturnsSqlProviderWithThatDriver(string driver, Type driverType)
    {
        var provider = Registry().ResolveSql(new DatasourceConfig { Kind = DatasourceKind.Sql, Driver = driver });

        provider.Driver.Should().BeOfType(driverType);
        provider.Capabilities.CanQuery.Should().BeTrue();
        provider.Capabilities.CanManageViews.Should().BeTrue();
    }

    [Fact]
    public void Resolve_ConfigWithoutKind_TreatedAsSql()
    {
        // Сохранённые до появления Kind настройки не несут типа источника.
        var provider = Registry().Resolve(new DatasourceConfig { Kind = "", Driver = "mysql" });

        provider.Should().BeAssignableTo<ISqlDatasourceProvider>();
    }

    [Fact]
    public void Resolve_UnknownKind_ReportsKind()
    {
        var action = () => Registry().Resolve(new DatasourceConfig { Kind = "rest", Driver = "" });

        action.Should().Throw<NotSupportedException>().WithMessage("*rest*");
    }

    [Fact]
    public void Resolve_DriverNotRegisteredForKind_ReportsDriver()
    {
        var action = () => Registry().Resolve(new DatasourceConfig { Kind = DatasourceKind.Sql, Driver = "oracle" });

        action.Should().Throw<NotSupportedException>().WithMessage("*oracle*");
    }

    [Fact]
    public void Resolve_KindWithSingleFactoryAndNoDriver_Resolves()
    {
        var registry = Registry(new FakeProviderFactory(DatasourceKind.File));

        registry.Resolve(new DatasourceConfig { Kind = DatasourceKind.File, Driver = "" })
            .Should().BeOfType<FakeProvider>();
    }

    [Fact]
    public void Resolve_KindWithSingleFactory_IgnoresStaleDriver()
    {
        // У конфига источника Driver по умолчанию "psql"; для типа без вариантов это не повод падать.
        var registry = Registry(new FakeProviderFactory(DatasourceKind.File));

        registry.Resolve(new DatasourceConfig { Kind = DatasourceKind.File, Driver = "psql" })
            .Should().BeOfType<FakeProvider>();
    }

    [Fact]
    public void Resolve_KindWithSeveralFactoriesAndNoDriver_ListsOptions()
    {
        var registry = Registry(new FakeProviderFactory("rest", "wp"), new FakeProviderFactory("rest", "openapi"));

        var action = () => registry.Resolve(new DatasourceConfig { Kind = "rest", Driver = "" });

        action.Should().Throw<NotSupportedException>().WithMessage("*wp*openapi*");
    }

    [Fact]
    public void ResolveSql_NonSqlKind_Refuses()
    {
        var registry = Registry(new FakeProviderFactory(DatasourceKind.File));

        var action = () => registry.ResolveSql(new DatasourceConfig { Kind = DatasourceKind.File, Driver = "" });

        action.Should().Throw<NotSupportedException>().WithMessage("*sql*");
    }

    [Fact]
    public void Describe_ContainsSqlDriversAndCustomProviders()
    {
        var described = Registry(new FakeProviderFactory(DatasourceKind.File)).Describe();

        described.Should().Contain(d => d.Kind == DatasourceKind.Sql && d.Driver == "psql");
        described.Should().Contain(d => d.Kind == DatasourceKind.Sql && d.Driver == "mssql");
        described.Should().Contain(d => d.Kind == DatasourceKind.Sql && d.Driver == "mysql");
        described.Should().Contain(d => d.Kind == DatasourceKind.File && d.Driver == "");

        described.Where(d => d.Kind == DatasourceKind.Sql)
            .Should().OnlyContain(d => d.DefaultConnectionString != "" && d.HelpLink != "");
    }
}

class FakeProviderFactory : IDatasourceProviderFactory
{
    public FakeProviderFactory(string kind, string driver = "")
    {
        Kind = kind;
        Driver = driver;
    }

    public string Kind { get; }
    public string Driver { get; }
    public string DisplayName => string.IsNullOrEmpty(Driver) ? Kind : $"{Kind}/{Driver}";
    public string DefaultConnectionString => "";
    public string HelpLink => "";

    public IDatasourceProvider Create(DatasourceConfig config) => new FakeProvider();
}

class FakeProvider : IDatasourceProvider
{
    public DatasourceCapabilities Capabilities { get; } = new() { CanQuery = true, CanBrowse = true };

    public Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default)
        => Task.FromResult(new DatasourceCatalog { Kind = "fake" });

    public Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new QueryResultDto { Ok = true });

    public Task<SqlNonQueryResultActionDto> Modify(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new SqlNonQueryResultActionDto { Ok = false, Message = "fake provider is read-only" });
}
