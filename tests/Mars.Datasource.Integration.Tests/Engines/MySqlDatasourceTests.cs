using Mars.Datasource.Core;
using Mars.Datasource.Host.MySQL;
using Mars.Datasource.Integration.Tests.Fixtures;
using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using MySql.Data.MySqlClient;

namespace Mars.Datasource.Integration.Tests.Engines;

public class MySqlDatasourceTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlDatasourceTests(MySqlFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset().Wait();
    }

    public DatasourceConfig Config()
    {
        return new DatasourceConfig
        {
            Driver = "mysql",
            Slug = "mysql",
            Title = "MySQL Server",
            ConnectionString = _fixture.ConnectionString,
        };
    }

    [IntegrationFact]
    public async Task CheckConnection()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

    }

    [IntegrationFact]
    public async Task CreateTable_Valid_ShouldSuccess()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task MySqlEngineTests()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);

        string query = "SELECT * FROM `todo` LIMIT 10";
        var se = new DatasourceMySQLDriver(Config());

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

    private static async Task<int> CreateTodoTableAsync(MySqlConnection connection)
    {
        var createTableSql = @"
            CREATE TABLE todo (
                Id CHAR(36) PRIMARY KEY DEFAULT (UUID()),
                Title VARCHAR(255) NOT NULL,
                Content TEXT,
                Completed BOOLEAN NOT NULL DEFAULT FALSE
            );";

        await using var command = new MySqlCommand(createTableSql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> CheckTableExistsAsync(MySqlConnection connection, string tableName)
    {
        // Query to check if the table exists in the current schema
        var checkTableSql = @"
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = DATABASE()
                  AND table_name = @tableName
            );";

        await using var command = new MySqlCommand(checkTableSql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);

        // Execute the query and return the result
        var result = await command.ExecuteScalarAsync();
        return Convert.ToBoolean(result);
    }

    [IntegrationFact]
    public async Task ClearTest()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);
        await _fixture.RemoveAllTablesAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeFalse();

    }
}
