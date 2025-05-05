using Mars.Datasource.Core.Dto;
using Mars.Datasource.Host.Controllers;
using Mars.Datasource.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.WebApiClient.Interfaces;
using FluentAssertions;
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
    public async Task TestConnection_Request_Success()
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
    public async Task SqlQuery_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.SqlQuery);
        _ = nameof(IDatasourceService.SqlQuery);
        var client = GetWebApiClient();

        //Act
        var result = await client.Datasource().SqlQuery("default", "SELECT COUNT(id) FROM posts");

        //Assert
        result.Ok.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task ExecuteAction_Request_Success()
    {
        //Arrange
        _ = nameof(DatasourceController.ExecuteAction);
        _ = nameof(IDatasourceService.ExecuteAction);
        var client = GetWebApiClient();
        var request = new ExecuteActionRequest { ActionId = "check_db_timezone", Arguments = [] };
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
