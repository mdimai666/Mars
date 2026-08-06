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
            // Read from wizard Setup config, fallback to defaults
            var siteUrl = configuration["Setup:SiteUrl"];
            var siteName = configuration["Setup:SiteName"] ?? "Mars";
            var siteDescription = configuration["Setup:SiteDescription"] ?? "New Mars website description";
            var adminEmail = configuration["Setup:AdminEmail"] ?? "admin@mail.localhost";

            if (string.IsNullOrEmpty(siteUrl))
            {
                var urls = string.IsNullOrEmpty(configuration["urls"]) ? "http://localhost" : configuration["urls"]!;
                OptionReaderTool.NormalizeUrl(urls, out siteUrl);
            }

            var sysOptions = new SysOptions
            {
                SiteUrl = siteUrl,
                AdminEmail = adminEmail,
                AllowUsersSelfRegister = false,
                SiteDescription = siteDescription,
                SiteName = siteName,
            };
            optionService.SaveOption(sysOptions);
        }

    }

}
