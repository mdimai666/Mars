using DotNet.Testcontainers.Builders;
using MySql.Data.MySqlClient;
using Testcontainers.MySql;

namespace Mars.Datasource.Integration.Tests.Fixtures;

public class MySqlFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container;
    //private Respawner _respawner = default!;

    public string ConnectionString => _container.GetConnectionString();

    public MySqlFixture()
    {
        _container = new MySqlBuilder()
            .WithName($"b-test-mysql-{Guid.NewGuid()}")
            .WithUsername("root")
            .WithPassword("password")
            .WithDatabase("test_db_source")
            .WithImage("mysql:8.0")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("mysqladmin", "ping", "-h", "localhost"))
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        //await using var connection = new MySqlConnection(_container.GetConnectionString());
        //await connection.OpenAsync();

        //_respawner = await Respawner.CreateAsync(connection,
        //    new RespawnerOptions { DbAdapter = DbAdapter.MySql });
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task Reset()
    {
        await using var connection = new MySqlConnection(_container.GetConnectionString() + ";Allow User Variables=True;");
        await connection.OpenAsync();
        await RemoveAllTablesAsync(connection);
        //await _respawner.ResetAsync(connection);
    }

    // Utils
    public async Task RemoveAllTablesAsync(MySqlConnection connection)
    {
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

    private async Task DisableForeignKeyChecksAsync(MySqlConnection connection)
    {
        var disableFkSql = "SET FOREIGN_KEY_CHECKS = 0;";
        await using var command = new MySqlCommand(disableFkSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnableForeignKeyChecksAsync(MySqlConnection connection)
    {
        var enableFkSql = "SET FOREIGN_KEY_CHECKS = 1;";
        await using var command = new MySqlCommand(enableFkSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string[]> GetAllTableNamesAsync(MySqlConnection connection)
    {
        var getTablesSql = "SELECT TABLE_NAME FROM information_schema.tables WHERE table_schema = DATABASE();";
        await using var command = new MySqlCommand(getTablesSql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var tableNames = new System.Collections.Generic.List<string>();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames.ToArray();
    }

    private async Task DropTableAsync(MySqlConnection connection, string tableName)
    {
        var dropTableSql = $"DROP TABLE `{tableName}`;";
        await using var command = new MySqlCommand(dropTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
