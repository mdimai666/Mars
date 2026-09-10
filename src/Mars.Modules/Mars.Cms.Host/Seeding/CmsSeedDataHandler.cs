using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Cms.Host.Seeding;

public class CmsSeedDataHandler : ISeedDataHandler
{
    public int Order => 10;

    public async Task SeedAsync(MarsDbContext dbContext, IServiceProvider services, IConfiguration configuration)
    {
        SeedRoles.SeedFirstData(dbContext);

        var userManager = services.GetRequiredService<UserManager<UserEntity>>();
        SeedUsers.SeedFirstData(userManager, dbContext, configuration);

        await SeedPostData.SeedFirstData(dbContext, services, configuration);
        SeedPostCategories.SeedFirstData(dbContext);
    }
}
