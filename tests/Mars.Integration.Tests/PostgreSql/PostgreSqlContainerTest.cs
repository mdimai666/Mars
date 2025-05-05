using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Mars.Integration.Tests.PostgreSql;

public sealed class PostgreSqlContainerTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder().Build();

    public Task InitializeAsync()
    {
        return _postgreSqlContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task ExecuteCommand()
    {
        using var connection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());

        await connection.OpenAsync();

        string sql = "SELECT 1";
        int expected = 1;

        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        var actual = reader.GetInt32(0);

        actual.Should().Be(expected);
    }
}