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
    public async Task DatabaseStructure_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.DatabaseStructure);
        _ = nameof(IDatasourceService.DatabaseStructure);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().DatabaseStructure("default");

        //Assert
        result.Should().NotBeNull();
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
        result.Select(d => d.Driver).Should().BeEquivalentTo(["psql", "mssql", "mysql"]);
        result.Should().OnlyContain(d => d.DefaultConnectionString != "" && d.HelpLink != "");
    }

    [IntegrationFact]
    public async Task RefreshStructure_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.RefreshStructure);
        _ = nameof(IDatasourceService.RefreshStructure);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().RefreshStructure("default");

        //Assert
        result.Tables.Should().NotBeEmpty();
    }

    [IntegrationFact]
    public async Task DatabaseStructure_CalledTwice_ReturnsCachedInstance()
    {
        //Arrange
        var first = await _datasourceService.DatabaseStructure("default");

        //Act
        var second = await _datasourceService.DatabaseStructure("default");

        //Assert
        second.Should().BeSameAs(first);
    }

    [IntegrationFact]
    public async Task Columns_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Columns);
        _ = nameof(IDatasourceService.Columns);
        var client = GetWebApiClient();
        var tables = await _datasourceService.Tables("default");

        //Act
        var result = await client.Datasource().Columns("default", tables.First().TableName);

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task ViewDefinition_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.ViewDefinition);
        _ = nameof(IDatasourceService.ViewDefinition);
        var client = GetWebApiClient();
        var view = $"ds_test_view_{Guid.NewGuid():N}";

        var created = await client.Datasource().NonQuery("default", new SqlRequest { Sql = $"CREATE VIEW \"{view}\" AS SELECT 1 AS id" });
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
            await client.Datasource().NonQuery("default", new SqlRequest { Sql = $"DROP VIEW \"{view}\"" });
        }
    }

    [IntegrationFact]
    public async Task Tables_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.Tables);
        _ = nameof(IDatasourceService.Tables);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().Tables("default");

        //Assert
        result.Should().NotBeNull();
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
        var result = await client.Datasource().Query("default", new SqlRequest { Sql = "SELECT COUNT(id) FROM posts" });

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
        var result = await client.Datasource().NonQuery("default", new SqlRequest { Sql = "CREATE TEMP TABLE ds_test (id int)" });

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
