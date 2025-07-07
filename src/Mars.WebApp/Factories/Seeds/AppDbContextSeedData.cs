using Mars.Host.Data.Contexts;
using Mars.Host.Options;
using Mars.Host.Shared.Services;
using Mars.Shared.Options;

namespace Mars.Factories.Seeds;

public static class AppDbContextSeedData
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
            var urls = string.IsNullOrEmpty(configuration["urls"]) ? "http://localhost" : configuration["urls"]!;
            var isValidUrl = OptionReaderTool.NormalizeUrl(urls, out var siteUrl);

            var sysOptions = new SysOptions
            {
                SiteUrl = siteUrl,
                AdminEmail = "admin@mail.localhost",
                AllowUsersSelfRegister = false,
                SiteDescription = "New Mars website description",
                SiteName = "Mars",
            };
            optionService.SaveOption(sysOptions);
        }

    }

}
