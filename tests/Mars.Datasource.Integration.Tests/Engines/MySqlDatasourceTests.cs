using FluentAssertions;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Providers.MySQL;
using Mars.Datasource.Integration.Tests.Fixtures;
using Mars.Datasource.Contracts.Models;
using Mars.Integration.Tests.Attributes;
using MySqlConnector;

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
    public async Task Open_ValidConnectionString_Connects()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

    }

    [IntegrationFact]
    public async Task CreateTable_Valid_Succeeds()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Driver_CreatedTodoTable_ReturnsRowsColumnsAndStructure()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        string query = "SELECT * FROM `todo` LIMIT 10";
        var se = new DatasourceMySQLDriver(Config());

        var result = await se.Query(new SqlRequest { Sql = query });
        result.Ok.Should().BeTrue(result.Message);
        result.Columns.Select(c => c.Name).Should().Equal("Id", "Title", "Content", "Completed");
        result.Rows.Should().HaveCount(2);
        result.Truncated.Should().BeFalse();

        var columns = await se.Columns("todo");
        columns.Values.Single(c => c.ColumnName == "Id").IsKey.Should().BeTrue();
        columns.Values.Single(c => c.ColumnName == "Title").IsNullable.Should().BeFalse();

        var tables = await se.Tables();
        tables.Should().Contain(t => t.TableName == "todo" && t.Kind == QTableKind.Table);

        var structure = await se.DatabaseStructure();
        var todo = structure.Tables.Single(t => t.TableName == "todo");
        todo.TableSchema.Kind.Should().Be(QTableKind.Table);
        todo.Columns.Values.Single(c => c.ColumnName == "Id").IsKey.Should().BeTrue();
        structure.DatabaseName.Should().NotBeNullOrEmpty();
    }

    [IntegrationFact]
    public async Task ViewDefinition_CreatedByBuilder_ReturnsBodyAndDrops()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);

        var se = new DatasourceMySQLDriver(Config());
        var view = $"todo_view_{Guid.NewGuid():N}";

        var create = ViewDdlBuilder.Create(SqlDialect.MySql, connection.Database, view, "SELECT Id, Title FROM todo", replace: false);
        create.Ok.Should().BeTrue(create.Error);

        var created = await se.NonQuery(create.Sql!);
        created.Ok.Should().BeTrue(created.Message);

        var definition = await se.ViewDefinition(connection.Database, view);
        definition.Should().NotBeNullOrWhiteSpace();
        definition.Should().Contain("todo");

        // Замена поверх существующей вьюхи проходит через CREATE OR REPLACE.
        var replace = ViewDdlBuilder.Create(SqlDialect.MySql, connection.Database, view, "SELECT Id FROM todo", replace: true);
        var replaced = await se.NonQuery(replace.Sql!);
        replaced.Ok.Should().BeTrue(replaced.Message);
        (await se.ViewDefinition(connection.Database, view)).Should().NotContain("Title");

        var drop = ViewDdlBuilder.Drop(SqlDialect.MySql, connection.Database, view);
        var dropped = await se.NonQuery(drop.Sql!);
        dropped.Ok.Should().BeTrue(dropped.Message);

        (await se.ViewDefinition(connection.Database, view)).Should().BeNull();
    }

    [IntegrationFact]
    public async Task Query_JsonColumn_MarksColumnAsJson()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using (var command = new MySqlCommand("CREATE TABLE todo_json (Id INT PRIMARY KEY, Data JSON)", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new MySqlCommand("""INSERT INTO todo_json (Id, Data) VALUES (1, '{"a": {"b": 1}}')""", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var se = new DatasourceMySQLDriver(Config());

        var result = await se.Query(new SqlRequest { Sql = "SELECT * FROM `todo_json`" });
        result.Ok.Should().BeTrue(result.Message);
        result.Columns.Single(c => c.Name == "Data").IsJson.Should().BeTrue();

        var columns = await se.Columns("todo_json");
        columns["Data"].IsJson.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Query_MaxRows_LimitsRowsAndMarksTruncated()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourceMySQLDriver(Config());

        var result = await se.Query(new SqlRequest { Sql = "SELECT * FROM `todo`", MaxRows = 1 });

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(1);
        result.Truncated.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task NonQuery_WithParameters_UpdatesRows()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourceMySQLDriver(Config());

        var result = await se.NonQuery(
            "UPDATE `todo` SET Title = @title WHERE Title = @from",
            [
                new SqlParam { Name = "title", Value = "edited" },
                new SqlParam { Name = "from", Value = "first" },
            ]);

        result.Ok.Should().BeTrue(result.Message);
        result.RowsAffected.Should().Be(1);
    }

    static async Task SeedTodoAsync(MySqlConnection connection)
    {
        await using var command = new MySqlCommand(
            "INSERT INTO todo (Title, Content, Completed) VALUES ('first', 'a', 0), ('second', 'b', 1)", connection);

        await command.ExecuteNonQueryAsync();
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
    public async Task RemoveAllTables_CreatedTable_TableDoesNotExist()
    {
        await using var connection = new MySqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);
        await _fixture.RemoveAllTablesAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeFalse();

    }
}
