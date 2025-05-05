using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Shared.Options;

namespace Mars.Data.Seeds;

public class AppDbContextSeedData
{
    public static void SeedFirstOption(
        MarsDbContext ef,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {

        var optionService = serviceProvider.GetRequiredService<IOptionService>();
        var existNewSysOption = optionService.GetOption<SysOptions>();

        if (string.IsNullOrEmpty(existNewSysOption.SiteUrl))
        {
            var saddr = configuration["urls"].Split(";").First();
            int port = new Uri(saddr).Port;

            var sysOptions = new SysOptions
            {
                SiteUrl = $"http://localhost:{port}",
                AdminEmail = "admin@mail.localhost",
                AllowUsersSelfRegister = false,
                SiteDescription = "New Mars website description",
                SiteName = "Mars",
            };
            optionService.SaveOption(sysOptions);
        }

    }
}
