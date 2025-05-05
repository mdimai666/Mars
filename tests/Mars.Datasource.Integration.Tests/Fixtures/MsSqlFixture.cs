using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Mars.Datasource.Integration.Tests.Fixtures;

public class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;
    //private Respawner _respawner = default!;

    public string ConnectionString => _container.GetConnectionString();

    public MsSqlFixture()
    {
        _container = new MsSqlBuilder()
            .WithName($"b-test-mssql-{Guid.NewGuid()}")
            //.WithUsername("sa") // SQL Server использует "sa" как пользователя по умолчанию
            .WithPassword("yourStrong(!)Password") // Пароль должен соответствовать требованиям сложности SQL Server
                                                   //.WithDatabase("test_db_source")
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest") // Используйте актуальный образ MSSQL
                                                                     //.WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("/opt/mssql-tools/bin/sqlcmd", "-Q", "SELECT 1;"))
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        //await using var connection = new SqlConnection(_container.GetConnectionString());
        //await connection.OpenAsync();

        //_respawner = await Respawner.CreateAsync(connection,
        //    new RespawnerOptions { DbAdapter = DbAdapter.SqlServer });
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task Reset()
    {
        await using var connection = new SqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await RemoveAllTablesAsync(connection);
        //await _respawner.ResetAsync(connection);
    }

    // Utils
    public async Task RemoveAllTablesAsync(SqlConnection connection)
    {
        //await using var connection = new SqlConnection(_container.GetConnectionString());
        //await connection.OpenAsync();

        // Disable foreign key checks to avoid issues with dependent tables
        await DisableForeignKeyChecksAsync(connection);

        // Get all table names in the database
        var tableNames = await GetAllTableNamesAsync(connection);

        // Drop each table
        foreach (var tableName in tableNames)
        {
            await DropTableAsync(connection, tableName);
            Console.WriteLine($"Dropped table: {tableName}");
        }

        // Re-enable foreign key checks
        await EnableForeignKeyChecksAsync(connection);
    }

    private async Task DisableForeignKeyChecksAsync(SqlConnection connection)
    {
        var disableFkSql = "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';";
        await using var command = new SqlCommand(disableFkSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnableForeignKeyChecksAsync(SqlConnection connection)
    {
        var enableFkSql = "EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';";
        await using var command = new SqlCommand(enableFkSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string[]> GetAllTableNamesAsync(SqlConnection connection)
    {
        var getTablesSql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';";
        await using var command = new SqlCommand(getTablesSql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var tableNames = new System.Collections.Generic.List<string>();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames.ToArray();
    }

    private async Task DropTableAsync(SqlConnection connection, string tableName)
    {
        var dropTableSql = $"DROP TABLE [{tableName}];";
        await using var command = new SqlCommand(dropTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
