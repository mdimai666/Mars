using Mars.Host.Data.Contexts;

namespace Mars.Host.Infrastructure.Services;

public class MarsDbContextFactory : IMarsDbContextFactory
{
    static internal string ConnectionString { get; set; } = default!;

    public MarsDbContext CreateInstance()
    {
        return MarsDbContext.CreateInstance(ConnectionString);
    }
}
