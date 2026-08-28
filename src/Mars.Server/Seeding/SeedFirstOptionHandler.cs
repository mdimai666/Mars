using Mars.Options.Services;
using Mars.Server.Contracts.Options;
using Mars.Server.Options;
using Microsoft.Extensions.Configuration;

namespace Mars.Server.Seeding;

public interface ISeedFirstOptionHandler
{
    void Seed(IConfiguration configuration);
}

public class SeedFirstOptionHandler(IOptionService optionService) : ISeedFirstOptionHandler
{
    public void Seed(IConfiguration configuration)
    {
        var siteSettings = optionService.GetOption<SiteSettings>();

        if (!string.IsNullOrEmpty(siteSettings.SiteUrl)) return;

        var setup = configuration.GetSection("Setup").Get<SetupSiteConfig>() ?? new();

        var siteUrl = setup.SiteUrl;
        var siteName = setup.SiteName ?? "Mars";
        var siteDescription = setup.SiteDescription ?? "New Mars website description";
        var adminEmail = setup.AdminEmail ?? "admin@mail.localhost";

        if (string.IsNullOrEmpty(siteUrl))
        {
            var urls = string.IsNullOrEmpty(configuration["urls"]) ? "http://localhost" : configuration["urls"]!;
            OptionReaderTool.NormalizeUrl(urls, out siteUrl);
        }

        optionService.SaveOption(new SiteSettings
        {
            SiteUrl = siteUrl,
            AdminEmail = adminEmail,
            AllowUsersSelfRegister = false,
            SiteDescription = siteDescription,
            SiteName = siteName,
        });
    }
}

public class SetupSiteConfig
{
    public string? SiteUrl { get; init; }
    public string? SiteName { get; init; }
    public string? SiteDescription { get; init; }
    public string? AdminEmail { get; init; }
}
