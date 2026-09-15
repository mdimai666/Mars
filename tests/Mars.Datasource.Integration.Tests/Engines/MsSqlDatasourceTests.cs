using FluentAssertions;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Host.MsSQL;
using Mars.Datasource.Integration.Tests.Fixtures;
using Mars.Integration.Tests.Attributes;
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
    public async Task Open_ValidConnectionString_Connects()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

    }

    [IntegrationFact]
    public async Task CreateTable_Valid_Succeeds()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Driver_CreatedTodoTable_ReturnsRowsColumnsAndStructure()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        string query = "SELECT TOP 10 * FROM [todo]";
        var se = new DatasourceMsSQLDriver(Config());

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
    public async Task Query_MaxRows_LimitsRowsAndMarksTruncated()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourceMsSQLDriver(Config());

        var result = await se.Query(new SqlRequest { Sql = "SELECT * FROM [todo]", MaxRows = 1 });

        result.Ok.Should().BeTrue(result.Message);
        result.Rows.Should().HaveCount(1);
        result.Truncated.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task NonQuery_WithParameters_UpdatesRows()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await CreateTodoTableAsync(connection);
        await SeedTodoAsync(connection);

        var se = new DatasourceMsSQLDriver(Config());

        var result = await se.NonQuery(
            "UPDATE [todo] SET Title = @title WHERE Title = @from",
            [
                new SqlParam { Name = "title", Value = "edited" },
                new SqlParam { Name = "from", Value = "first" },
            ]);

        result.Ok.Should().BeTrue(result.Message);
        result.RowsAffected.Should().Be(1);
    }

    static async Task SeedTodoAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand(
            "INSERT INTO todo (Title, Content, Completed) VALUES ('first', 'a', 0), ('second', 'b', 1)", connection);

        await command.ExecuteNonQueryAsync();
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
    public async Task RemoveAllTables_CreatedTable_TableDoesNotExist()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await CreateTodoTableAsync(connection);
        await _fixture.RemoveAllTablesAsync(connection);

        bool tableExists = await CheckTableExistsAsync(connection, "todo");
        tableExists.Should().BeFalse();

    }
}
