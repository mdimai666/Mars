using FluentAssertions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Mars.Integration.Tests.PostgreSql;

public sealed class PostgreSqlContainerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:14").Build();

    public ValueTask InitializeAsync()
    {
        return new ValueTask(_postgreSqlContainer.StartAsync());
    }

    public ValueTask DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteCommand_SelectOne_ReturnsOne()
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
