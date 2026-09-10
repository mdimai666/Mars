using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Contexts;

public interface IMarsDbContextFactory
{
    void OptionsBuilderAction(DbContextOptionsBuilder optionsBuilder);
    MarsDbContext CreateInstance();
    void OnModelCreating(ModelBuilder builder);
}
