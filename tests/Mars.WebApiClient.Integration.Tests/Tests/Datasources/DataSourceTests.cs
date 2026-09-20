using FluentAssertions;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Front.Services;
using Mars.Datasource.Host.Controllers;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.Datasources;

public class DataSourceTests : BaseWebApiClientTests
{
    private readonly IDatasourceService _datasourceService;
    private readonly IDatasourceRegistry _datasourceRegistry;

    public DataSourceTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _datasourceService = appFixture.ServiceProvider.GetRequiredService<IDatasourceService>();
        _datasourceRegistry = appFixture.ServiceProvider.GetRequiredService<IDatasourceRegistry>();
    }

    [IntegrationFact]
    public async Task Providers_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Providers);
        _ = nameof(IDatasourceRegistry.Providers);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().Providers();

        //Assert
        result.Where(d => d.Kind == DatasourceKind.Sql).Select(d => d.Driver).Should().BeEquivalentTo(["psql", "mssql", "mysql"]);
        result.Where(d => d.Kind == DatasourceKind.Sql)
            .Should().OnlyContain(d => d.DefaultConnectionString != "" && d.HelpLink != "");

        // Файловый провайдер подключается тем же реестром: у него нет ни движка, ни строки подключения.
        result.Should().Contain(d => d.Kind == DatasourceKind.File && d.Driver == "");
        result.Should().Contain(d => d.Kind == DatasourceKind.Rest && d.Driver == "");

        // Поля формы настроек объявляет провайдер: по ним рисуется общая форма.
        result.Single(d => d.Kind == DatasourceKind.Rest)
            .Settings.Should().Contain(field => field.Editor == DatasourceSettingEditor.Auth);
    }

    [IntegrationFact]
    public async Task TestConnection_RestWithoutDiscovery_Succeeds()
    {
        //Arrange
        _ = nameof(DatasourceController.TestConnection);
        var client = GetWebApiClient();
        var connection = new ConnectionStringTestDto
        {
            Kind = DatasourceKind.Rest,
            Settings = new Dictionary<string, string> { [DatasourceSettings.Discovery] = RestDiscovery.None },
        };

        //Act
        var result = await client.Datasource().TestConnection(connection);

        //Assert
        result.Ok.Should().BeTrue(result.Message);
        result.Message.Should().Contain("0 objects");
    }

    [IntegrationFact]
    public async Task TestConnection_RestWithoutBaseUrl_ReportsMissingAddress()
    {
        //Arrange
        var client = GetWebApiClient();
        var connection = new ConnectionStringTestDto { Kind = DatasourceKind.Rest };

        //Act
        var result = await client.Datasource().TestConnection(connection);

        //Assert
        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("Не задан адрес API");
    }

    [IntegrationFact]
    public void Document_OfSourceWithoutDocument_IsRefused()
    {
        //Arrange
        _ = nameof(DatasourceController.Document);
        _ = nameof(DatasourceController.SaveDocument);
        _ = nameof(IDatasourceService.Document);
        _ = nameof(IDatasourceService.SaveDocument);

        // У sql-источника документа запросов нет: имя из запроса не должно писать в data-корень.
        var action = () => _datasourceService.Document("default", DatasourceSettings.RequestsDocument);

        //Act & Assert
        action.Should().ThrowAsync<NotSupportedException>().WithMessage("*не хранит документ*");
    }

    [IntegrationFact]
    public async Task Catalog_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Catalog);
        _ = nameof(IDatasourceService.Catalog);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().Catalog("default");

        //Assert
        result.Profile.Kind.Should().Be(DatasourceKind.Sql);
        result.Groups.Should().NotBeEmpty();
        result.Groups.SelectMany(g => g.Objects).Should().NotBeEmpty();
        result.Profile.Has(DatasourceFeature.Query).Should().BeTrue();
        result.Profile.Has(DatasourceFeature.Views).Should().BeTrue();

        var posts = result.Groups.SelectMany(g => g.Objects).FirstOrDefault(o => o.Name == "posts");
        posts.Should().NotBeNull();
        posts!.Id.Should().Be("public.posts");
        posts.DefaultLanguage.Should().Be(DatasourceLanguage.Sql);
        posts.Fields.Should().NotBeEmpty();
        posts.Fields.Should().Contain(c => c.IsKey);

        // Утилиты движка приходят в каталоге: страница рисует кнопки из этого списка.
        result.Actions.Select(action => action.Id).Should().Contain("check_db_timezone");
    }

    [IntegrationFact]
    public async Task Catalog_CalledTwice_ReturnsCachedInstance()
    {
        //Arrange
        var first = await _datasourceService.Catalog("default");

        //Act
        var second = await _datasourceService.Catalog("default");

        //Assert
        second.Should().BeSameAs(first);
    }

    [IntegrationFact]
    public async Task Catalog_Refresh_ReplacesCache()
    {
        //Arrange
        _ = nameof(IDatasourceService.RefreshCatalog);
        var client = GetWebApiClient();

        var cached = await _datasourceService.Catalog("default");

        //Act
        var refreshed = await client.Datasource().Catalog("default", refresh: true);

        //Assert
        refreshed.Groups.Should().NotBeEmpty();
        refreshed.Should().NotBeSameAs(cached);
        // Кэш заменён: следующий вызов сервиса отдаёт перечитанный каталог, а не старый.
        (await _datasourceService.Catalog("default")).Should().NotBeSameAs(cached);
    }

    [IntegrationFact]
    public async Task ViewDefinition_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.ViewDefinition);
        _ = nameof(ISqlDatasourceService.ViewDefinition);
        var client = GetWebApiClient();
        var view = $"ds_test_view_{Guid.NewGuid():N}";

        var created = await client.Datasource().Modify("default", new DatasourceRequest { Query = $"CREATE VIEW \"{view}\" AS SELECT 1 AS id" });
        created.Ok.Should().BeTrue(created.Message);

        try
        {
            //Act
            var result = await client.Datasource().ViewDefinition("default", null, view);

            //Assert
            result.Sql.Should().Contain("id");
        }
        finally
        {
            await client.Datasource().Modify("default", new DatasourceRequest { Query = $"DROP VIEW \"{view}\"" });
        }
    }

    [IntegrationFact]
    public async Task TestConnection_DefaultPsqlConnection_DoesNotThrow()
    {
        //Arrange
        _ = nameof(DatasourceController.TestConnection);
        _ = nameof(IDatasourceRegistry.TestConnection);
        var client = GetWebApiClient();
        var connection = new ConnectionStringTestDto { ConnectionString = "default", Driver = "psql" };

        //Act
        var action = () => client.Datasource().TestConnection(connection);

        //Assert
        await action.Should().NotThrowAsync();
    }

    [IntegrationFact]
    public async Task Query_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Query);
        _ = nameof(IDatasourceService.Query);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().Query("default", new DatasourceRequest { Query = "SELECT COUNT(id) FROM posts" });

        //Assert
        result.Ok.Should().BeTrue(result.Message);
        result.Fields.Should().NotBeEmpty();
        result.Rows.Should().HaveCount(1);
    }

    [IntegrationFact]
    public async Task Modify_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Modify);
        _ = nameof(IDatasourceService.Modify);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().Modify("default", new DatasourceRequest { Query = "CREATE TEMP TABLE ds_test (id int)" });

        //Assert
        result.Ok.Should().BeTrue(result.Message);
    }

    [IntegrationFact]
    public async Task ExecuteAction_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.ExecuteAction);
        _ = nameof(IDatasourceService.ExecuteAction);
        var client = GetWebApiClient();
        var request = new DatasourceActionRequest { ActionId = "check_db_timezone", Arguments = [] };

        //Act
        var result = await client.Datasource().ExecuteAction("default", request);

        //Assert
        result.Ok.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task ListSelectDatasource_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.ListSelectDatasource);
        _ = nameof(IDatasourceRegistry.ListSelectDatasource);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().ListSelectDatasource();

        //Assert
        result.Count.Should().BeGreaterThan(0);
    }
}
