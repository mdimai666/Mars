using Mars.Host.Data.Contexts;
using Mars.Host.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mars.Host.Data.InMemory;

public class MarsDbContextInMemoryFactory(DatabaseConnectionOpt connectionOpt) : IMarsDbContextFactory
{

    public void OptionsBuilderAction(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(connectionOpt.ConnectionString);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new MarsDbContextOptionExtension(this));

        //optionsBuilder.EnableDetailedErrors();
    }

    public MarsDbContext CreateInstance()
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarsDbContext>();
        OptionsBuilderAction(optionsBuilder);
        return new MarsDbContext(optionsBuilder.Options);
    }

    public void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
