using FluentAssertions;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Host.PostgreSQL;
using Mars.Datasource.Integration.Tests.Fixtures;
using Mars.Datasource.Integration.Tests.SqlCommands;
using Mars.Integration.Tests.Attributes;
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
    public async Task Open_ValidConnectionString_Connects()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

    }

    [IntegrationFact]
    public async Task CreateTable_Valid_Succeeds()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeTrue();

    }

    [IntegrationFact]
    public async Task Driver_CreatedTodoTable_ReturnsRowsColumnsAndStructure()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        string query = "SELECT * FROM \"todo\"";
        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.Query(new SqlRequest { Sql = query });
        result.Ok.Should().BeTrue(result.Message);
        result.Columns.Select(c => c.Name).Should().Equal("id", "title", "content", "completed");
        result.Rows.Should().HaveCount(2);
        result.Truncated.Should().BeFalse();

        var columns = await se.Columns("todo");
        Assert.True(columns.Count > 0);

        var tables = await se.Tables();
        Assert.True(tables.Count > 0);

        var structure = await se.DatabaseStructure();
        Assert.True(structure.Tables.Count > 0);
        Assert.NotNull(structure.DatabaseName);
    }

    [IntegrationFact]
    public async Task Query_MaxRows_LimitsRowsAndMarksTruncated()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.Query(new SqlRequest { Sql = "SELECT * FROM \"todo\"", MaxRows = 1 });

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(1);
        result.Truncated.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Query_InvalidSql_ReturnsError()
    {
        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.Query(new SqlRequest { Sql = "SELECT * FROM no_such_table" });

        result.Ok.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [IntegrationFact]
    public async Task NonQuery_WithParameters_UpdatesRows()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.NonQuery(
            "UPDATE \"todo\" SET title = @title WHERE title = @from",
            [
                new SqlParam { Name = "title", Value = "edited" },
                new SqlParam { Name = "from", Value = "first" },
            ]);

        result.Ok.Should().BeTrue(result.Message);
        result.RowsAffected.Should().Be(1);
    }

    static async Task SeedTodoAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            "INSERT INTO todo (title, content, completed) VALUES ('first', 'a', false), ('second', 'b', true)", connection);

        await command.ExecuteNonQueryAsync();
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
    public async Task RemoveAllTables_CreatedTable_TableDoesNotExist()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);
        await _fixture.RemoveAllTablesAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeFalse();

    }
}
