using FluentAssertions;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Providers.PostgreSQL;
using Mars.Datasource.Integration.Tests.Fixtures;
using Mars.Datasource.Integration.Tests.SqlCommands;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
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

        var result = await se.Query(new DatasourceRequest { Query = query });
        result.Ok.Should().BeTrue(result.Message);
        result.Fields.Select(c => c.Name).Should().Equal("id", "title", "content", "completed");
        result.Rows.Should().HaveCount(2);
        result.Truncated.Should().BeFalse();

        var columns = await se.Columns("todo");
        columns.Values.Single(c => c.ColumnName == "id").IsKey.Should().BeTrue();
        columns.Values.Single(c => c.ColumnName == "title").IsNullable.Should().BeFalse();

        var tables = await se.Tables();
        tables.Should().Contain(t => t.TableName == "todo" && t.Kind == QTableKind.Table);

        var structure = await se.DatabaseStructure();
        var todo = structure.Tables.Single(t => t.TableName == "todo");
        todo.TableSchema.Kind.Should().Be(QTableKind.Table);
        todo.Columns.Values.Single(c => c.ColumnName == "id").IsKey.Should().BeTrue();
        structure.DatabaseName.Should().NotBeNullOrEmpty();
    }

    [IntegrationFact]
    public async Task DatabaseStructure_CreatedView_AppearsWithViewKind()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);

        string viewName = $"todo_view_{Guid.NewGuid():N}";
        await using (var command = new NpgsqlCommand($"CREATE VIEW \"{viewName}\" AS SELECT * FROM todo", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var se = new DatasourcePostgreSQLDriver(Config());
        var structure = await se.DatabaseStructure();

        var view = structure.Tables.Single(t => t.TableName == viewName);
        view.TableSchema.Kind.Should().Be(QTableKind.View);
        view.TableSchema.IsView.Should().BeTrue();
        view.Columns.Should().NotBeEmpty();
    }

    [IntegrationFact]
    public async Task ViewDefinition_CreatedByBuilder_ReturnsBodyAndDrops()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);

        var se = new DatasourcePostgreSQLDriver(Config());
        var view = $"todo_view_{Guid.NewGuid():N}";

        var create = ViewDdlBuilder.Create(SqlDialect.Postgres, "public", view, "SELECT id, title FROM todo", replace: false);
        create.Ok.Should().BeTrue(create.Error);

        var created = await se.NonQuery(create.Sql!);
        created.Ok.Should().BeTrue(created.Message);

        var definition = await se.ViewDefinition("public", view);
        definition.Should().NotBeNullOrWhiteSpace();
        definition.Should().Contain("todo");

        // Замена поверх существующей вьюхи проходит через OR REPLACE: колонки можно добавлять.
        var replace = ViewDdlBuilder.Create(SqlDialect.Postgres, "public", view, "SELECT id, title, content FROM todo", replace: true);
        var replaced = await se.NonQuery(replace.Sql!);
        replaced.Ok.Should().BeTrue(replaced.Message);
        (await se.ViewDefinition("public", view)).Should().Contain("content");

        // А убрать колонку Postgres через OR REPLACE не даёт — это ожидаемая ошибка движка,
        // а не повод удалять вьюху за спиной пользователя (решение: drop+create не делаем).
        var shrink = ViewDdlBuilder.Create(SqlDialect.Postgres, "public", view, "SELECT id FROM todo", replace: true);
        var shrunk = await se.NonQuery(shrink.Sql!);
        shrunk.Ok.Should().BeFalse();
        shrunk.Message.Should().NotBeNullOrWhiteSpace();
        (await se.ViewDefinition("public", view)).Should().Contain("content");

        var drop = ViewDdlBuilder.Drop(SqlDialect.Postgres, "public", view);
        var dropped = await se.NonQuery(drop.Sql!);
        dropped.Ok.Should().BeTrue(dropped.Message);

        (await se.ViewDefinition("public", view)).Should().BeNull();
        (await se.ViewDefinition("public", "no_such_view")).Should().BeNull();
    }

    [IntegrationFact]
    public async Task Query_JsonColumn_MarksColumnAsJson()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using (var command = new NpgsqlCommand("CREATE TABLE todo_json (id int PRIMARY KEY, data jsonb)", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new NpgsqlCommand("""INSERT INTO todo_json (id, data) VALUES (1, '{"a": {"b": 1}}')""", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.Query(new DatasourceRequest { Query = "SELECT * FROM todo_json" });
        result.Ok.Should().BeTrue(result.Message);
        result.Fields.Single(c => c.Name == "data").IsJson.Should().BeTrue();
        result.Fields.Single(c => c.Name == "id").IsJson.Should().BeFalse();

        var columns = await se.Columns("todo_json");
        columns["data"].IsJson.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task RowUpdatePlan_ExecutedByNonQuery_UpdatesRow()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.Query(new DatasourceRequest { Query = "SELECT id, title FROM todo ORDER BY title" });
        result.Ok.Should().BeTrue(result.Message);

        var idIndex = Array.FindIndex(result.Fields, c => c.Name == "id");

        // Так же, как это делает грид: ключ и новое значение — строки из результата запроса.
        var plan = RowUpdateBuilder.Build(
            "public",
            "todo",
            name => $"\"{name}\"",
            ["id"],
            new Dictionary<string, string?> { ["id"] = result.Rows[0][idIndex] },
            new Dictionary<string, string?> { ["title"] = "edited" });

        plan.Should().NotBeNull();

        var nonQuery = await se.NonQuery(plan!.Sql, plan.Parameters);

        nonQuery.Ok.Should().BeTrue(nonQuery.Message);
        nonQuery.RowsAffected.Should().Be(1);

        var after = await se.Query(new DatasourceRequest { Query = "SELECT title FROM todo WHERE title = 'edited'" });
        after.Rows.Should().HaveCount(1);
    }

    [IntegrationFact]
    public async Task Query_MaxRows_LimitsRowsAndMarksTruncated()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.Query(new DatasourceRequest { Query = "SELECT * FROM \"todo\"", MaxRows = 1 });

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(1);
        result.Truncated.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Query_InvalidSql_ReturnsError()
    {
        var se = new DatasourcePostgreSQLDriver(Config());

        var result = await se.Query(new DatasourceRequest { Query = "SELECT * FROM no_such_table" });

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
                new DatasourceParam { Name = "title", Value = "edited" },
                new DatasourceParam { Name = "from", Value = "first" },
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
