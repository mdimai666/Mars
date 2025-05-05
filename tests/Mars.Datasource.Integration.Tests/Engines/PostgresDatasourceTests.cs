using Mars.Datasource.Core;
using Mars.Datasource.Host.PostgreSQL;
using Mars.Datasource.Integration.Tests.Fixtures;
using Mars.Datasource.Integration.Tests.SqlCommands;
using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using Npgsql;

namespace Mars.Datasource.Integration.Tests.Engines;

public class PostgresDatasourceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresDatasourceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset().Wait();
    }

    public DatasourceConfig Config()
    {
        return new DatasourceConfig
        {
            Driver = "psql",
            Slug = "psql",
            Title = "My postgres",
            ConnectionString = _fixture.ConnectionString,
        };
    }

    [IntegrationFact]
    public async Task CheckConnection()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

    }

    [IntegrationFact]
    public async Task CreateTable_Valid_ShouldSuccess()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeTrue();

    }

    [IntegrationFact]
    public async void PostgresEngineTests()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);

        string query = "SELECT * FROM \"todo\" LIMIT 10";
        var se = new DatasourcePostgreSQLDriver(Config());

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

    private static async Task<int> CreateTodoTableAsync(NpgsqlConnection connection)
    {
        var createTableSql = AssetUtils.GetSqlCommandScript("/Postgres/CreateTodoTable.sql");

        await using var command = new NpgsqlCommand(createTableSql, connection);

        return await command.ExecuteNonQueryAsync();
        //Console.WriteLine("Table 'Todo' created successfully.");
    }

    private static async Task<bool> CheckTableExistsAsync(NpgsqlConnection connection, string tableName)
    {
        // Query to check if the table exists in the current schema
        var checkTableSql = @"
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_name = @tableName
            );";

        await using var command = new NpgsqlCommand(checkTableSql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);

        // Execute the query and return the result
        var result = await command.ExecuteScalarAsync();
        return (bool)result!;
    }

    [IntegrationFact]
    public async Task ClearTest()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);
        await _fixture.RemoveAllTablesAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeFalse();

    }
}
