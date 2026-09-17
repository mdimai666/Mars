using FluentAssertions;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Abstractions.Services;
using Mars.Datasource.Front.Services;
using Mars.Datasource.Host.Controllers;
using Mars.Datasource.Contracts.Models;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.Datasources;

public class DataSourceTests : BaseWebApiClientTests
{
    private IDatasourceService _datasourceService;

    public DataSourceTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _datasourceService = appFixture.ServiceProvider.GetRequiredService<IDatasourceService>();
    }

    [IntegrationFact]
    public async Task Drivers_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Drivers);
        _ = nameof(IDatasourceService.Drivers);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().Drivers();

        //Assert
        result.Where(d => d.Kind == DatasourceKind.Sql).Select(d => d.Driver).Should().BeEquivalentTo(["psql", "mssql", "mysql"]);
        result.Where(d => d.Kind == DatasourceKind.Sql)
            .Should().OnlyContain(d => d.DefaultConnectionString != "" && d.HelpLink != "");

        // Файловый провайдер подключается тем же реестром: у него нет ни движка, ни строки подключения.
        result.Should().Contain(d => d.Kind == DatasourceKind.File && d.Driver == "");
        result.Should().Contain(d => d.Kind == DatasourceKind.Rest && d.Driver == "");
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
    public async Task Requests_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Requests);
        _ = nameof(IDatasourceService.RequestsDocument);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().Requests("default");

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task Requests_SaveAndRead_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.SaveRequests);
        _ = nameof(IDatasourceService.SaveRequestsDocument);
        var client = GetWebApiClient();
        var content = "###\n# @name smoke\nGET {{baseUrl}}/wp-json/wp/v2/posts\n";

        //Act
        var saved = await client.Datasource().SaveRequests("default", content);

        //Assert
        saved.Ok.Should().BeTrue(saved.Message);
        (await client.Datasource().Requests("default")).Should().Be(content);
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
        result.Kind.Should().Be(DatasourceKind.Sql);
        result.Groups.Should().NotBeEmpty();
        result.Groups.SelectMany(g => g.Objects).Should().NotBeEmpty();
        result.Capabilities.CanQuery.Should().BeTrue();
        result.Capabilities.CanManageViews.Should().BeTrue();

        var posts = result.Groups.SelectMany(g => g.Objects).FirstOrDefault(o => o.Name == "posts");
        posts.Should().NotBeNull();
        posts!.Id.Should().Be("public.posts");
        posts.DefaultLanguage.Should().Be(DatasourceLanguage.Sql);
        posts.Columns.Should().NotBeEmpty();
        posts.Columns.Should().Contain(c => c.IsKey);
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
    public async Task RefreshCatalog_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.RefreshCatalog);
        _ = nameof(IDatasourceService.RefreshCatalog);
        var client = GetWebApiClient();

        var cached = await _datasourceService.Catalog("default");

        //Act
        var refreshed = await client.Datasource().RefreshCatalog("default");

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
        _ = nameof(IDatasourceService.ViewDefinition);
        var client = GetWebApiClient();
        var view = $"ds_test_view_{Guid.NewGuid():N}";

        var created = await client.Datasource().NonQuery("default", new DatasourceRequest { Query = $"CREATE VIEW \"{view}\" AS SELECT 1 AS id" });
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
            await client.Datasource().NonQuery("default", new DatasourceRequest { Query = $"DROP VIEW \"{view}\"" });
        }
    }

    [IntegrationFact]
    public async Task TestConnection_DefaultPsqlConnection_DoesNotThrow()
    {
        //Arrange
        _ = nameof(DatasourceController.TestConnection);
        _ = nameof(IDatasourceService.TestConnection);
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
        result.Columns.Should().NotBeEmpty();
        result.Rows.Should().HaveCount(1);
    }

    [IntegrationFact]
    public async Task NonQuery_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.NonQuery);
        _ = nameof(IDatasourceService.NonQuery);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().NonQuery("default", new DatasourceRequest { Query = "CREATE TEMP TABLE ds_test (id int)" });

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
        _ = nameof(IDatasourceService.ListSelectDatasource);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().ListSelectDatasource();

        //Assert
        result.Count.Should().BeGreaterThan(0);
    }
}
