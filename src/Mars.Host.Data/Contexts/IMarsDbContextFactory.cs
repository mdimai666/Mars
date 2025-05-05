namespace Mars.Host.Data.Contexts;

public interface IMarsDbContextFactory
{
    public MarsDbContext CreateInstance();
}
