using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Mars.Datasource.Integration.Tests.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    //private Respawner _respawner = default!;

    public string ConnectionString => _container.GetConnectionString();

    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithName($"b-test-postgres-{Guid.NewGuid()}")
            .WithUsername("postgres1")
            .WithPassword("postgres1")
            .WithDatabase("test_db_source")
            .WithImage("postgres:14")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        //await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        //await connection.OpenAsync();

        //_respawner = await Respawner.CreateAsync(connection,
        //    new RespawnerOptions { DbAdapter = DbAdapter.Postgres });
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task Reset()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString() + ";Include Error Detail=True;");
        await connection.OpenAsync();
        await RemoveAllTablesAsync(connection);
        //await _respawner.ResetAsync(connection);
    }

    // Utils
    public async Task RemoveAllTablesAsync(NpgsqlConnection connection)
    {
        // Disable foreign key checks (PostgreSQL does not have a direct equivalent, so we drop constraints)
        await DropAllForeignKeysAsync(connection);

        // Get all table names in the database
        var tableNames = await GetAllTableNamesAsync(connection);

        // Drop each table
        foreach (var tableName in tableNames)
        {
            await DropTableAsync(connection, tableName);
            Console.WriteLine($"Dropped table: {tableName}");
        }
    }

    private async Task DropAllForeignKeysAsync(NpgsqlConnection connection)
    {
        // Drop all foreign key constraints in the database
        var dropFkSql = @"
            DO $$ DECLARE
                r RECORD;
            BEGIN
                FOR r IN (
                    SELECT conname, conrelid::regclass AS table_name
                    FROM pg_constraint
                    WHERE contype = 'f'
                ) LOOP
                    EXECUTE 'ALTER TABLE ' || r.table_name || ' DROP CONSTRAINT ' || r.conname || ';';
                END LOOP;
            END $$;";

        await using var command = new NpgsqlCommand(dropFkSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string[]> GetAllTableNamesAsync(NpgsqlConnection connection)
    {
        var getTablesSql = @"
            SELECT tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname = 'public';"; // Change 'public' to your schema name if needed

        await using var command = new NpgsqlCommand(getTablesSql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var tableNames = new System.Collections.Generic.List<string>();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames.ToArray();
    }

    private async Task DropTableAsync(NpgsqlConnection connection, string tableName)
    {
        var dropTableSql = $"DROP TABLE IF EXISTS \"{tableName}\" CASCADE;";
        await using var command = new NpgsqlCommand(dropTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
