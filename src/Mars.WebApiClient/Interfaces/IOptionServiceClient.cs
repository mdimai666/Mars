using Mars.Core.Models;
using Mars.Server.Contracts.Options;

namespace Mars.WebApiClient.Interfaces;

public interface IOptionServiceClient
{
    Task<SiteSettings> GetSysOptions();
    Task SaveSysOptions(SiteSettings value);
    Task<T?> GetOption<T>();
    Task SaveOption<T>(T value);
    Task SetLanguage(string culture, string returnUrl);

}
