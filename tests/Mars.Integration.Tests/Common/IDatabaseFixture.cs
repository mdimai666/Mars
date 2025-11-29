using Mars.Host.Data.Contexts;

namespace Mars.Integration.Tests.Common;

public interface IDatabaseFixture : IAsyncLifetime
{
    MarsDbContext DbContext { get; }
    string ConnectionString { get; }
    Task Reset();
}
