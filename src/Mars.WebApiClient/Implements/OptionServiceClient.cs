using Flurl.Http;
using Mars.Server.Contracts.Options;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class OptionServiceClient : BasicServiceClient, IOptionServiceClient
{
    public OptionServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Option";
    }

    public Task<SiteSettings> GetSiteSettings()
        => _client.Request($"{_basePath}{_controllerName}", "SiteSettings")
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<SiteSettings>();

    public Task SaveSiteSettings(SiteSettings value)
        => _client.Request($"{_basePath}{_controllerName}", "SiteSettings")
                    .PutJsonAsync(value);

    public Task<T?> GetOption<T>()
        => _client.Request($"{_basePath}{_controllerName}", "Option", typeof(T).Name)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<T?>();

    public Task SaveOption<T>(T value)
        => _client.Request($"{_basePath}{_controllerName}", "Option", typeof(T).Name)
                    .PutJsonAsync(value);

    public Task SetLanguage(string culture, string returnUrl)
    {
        throw new NotImplementedException();
    }

}
