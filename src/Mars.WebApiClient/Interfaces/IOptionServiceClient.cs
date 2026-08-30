using Mars.Server.Contracts.Options;

namespace Mars.WebApiClient.Interfaces;

public interface IOptionServiceClient
{
    Task<SiteSettings> GetSiteSettings();
    Task SaveSiteSettings(SiteSettings value);
    Task<T?> GetOption<T>();
    Task SaveOption<T>(T value);
    Task SetLanguage(string culture, string returnUrl);

}
