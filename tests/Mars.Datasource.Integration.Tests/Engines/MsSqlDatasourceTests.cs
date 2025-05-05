using Mars.Datasource.Core;
using Mars.Datasource.Host.MsSQL;
using Mars.Datasource.Integration.Tests.Fixtures;
using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace Mars.Datasource.Integration.Tests.Engines;

public class MsSqlDatasourceTests : IClassFixture<MsSqlFixture>
{
    private readonly MsSqlFixture _fixture;

    public MsSqlDatasourceTests(MsSqlFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset().Wait();
    }

    public DatasourceConfig Config()
    {
        return new DatasourceConfig
        {
            Driver = "mssql",
            Slug = "mssql",
            Title = "My SQLServer",
            ConnectionString = _fixture.ConnectionString,
        };
    }

    [IntegrationFact]
    public async Task CheckConnection()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

    }

    [IntegrationFact]
    public async Task CreateTable_Valid_ShouldSuccess()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task MsSqlEngineTests()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);

        string query = "SELECT TOP 10 * FROM [todo]";
        var se = new DatasourceMsSQLDriver(Config());

        var result = await se.SqlQuery(query);
        Assert.True(result.Data.Length > 0);

        var columns = await se.Columns("todo");
        Assert.True(columns.Count > 0);

        var tables = await se.Tables();
        Assert.True(tables.Count > 0);

        var structure = await se.DatabaseStructure();
        Assert.True(structure.Tables.Count > 0);
        Assert.NotNull(structure.DatabaseName);
    }

    private static async Task<int> CreateTodoTableAsync(SqlConnection connection)
    {
        var createTableSql = @"
            CREATE TABLE todo (
                Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                Title NVARCHAR(255) NOT NULL,
                Content NVARCHAR(MAX),
                Completed BIT NOT NULL DEFAULT 0
            );";

        await using var command = new SqlCommand(createTableSql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> CheckTableExistsAsync(SqlConnection connection, string tableName)
    {
        // Query to check if the table exists in the current schema
        var checkTableSql = @"
            SELECT CASE 
                WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = @tableName
                ) THEN 1
                ELSE 0
            END;";

        await using var command = new SqlCommand(checkTableSql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);

        // Execute the query and return the result
        var result = await command.ExecuteScalarAsync();
        return Convert.ToBoolean(result);
    }

    [IntegrationFact]
    public async Task ClearTest()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);
        await _fixture.RemoveAllTablesAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeFalse();

    }
}
