using Mars.Server.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Factories.Seeds;

public static class AppDbContextSeedData
{
    public static void SeedFirstOption(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        serviceProvider.GetRequiredService<ISeedFirstOptionHandler>().Seed(configuration);
    }
}
