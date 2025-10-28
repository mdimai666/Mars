using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;
using Mars.SSO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SSO.Providers;

/// <summary>
/// DynamicSsoProvider factory that creates provider instances from descriptors
/// </summary>
public class DynamicSsoProviderFactory
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OidcMetadataCache _metadataCache;
    private readonly IConfiguration _config; // for local provider config
    private readonly IServiceProvider _serviceProvider;

    public DynamicSsoProviderFactory(IHttpClientFactory httpFactory,
                                    OidcMetadataCache metadataCache,
                                    IConfiguration config,
                                    IServiceProvider serviceProvider)
    {
        _httpFactory = httpFactory;
        _metadataCache = metadataCache;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    public ISsoProvider Create(SsoProviderDescriptor desc)
    {
        // choose the implementation by descriptor.Name or other hints
        var name = desc.Name?.ToLowerInvariant();
        var driver = desc.Driver;
        //if (driver == "keycloak") return new KeycloakProvider2(desc, _httpFactory, _metadataCache);
        //if (driver == "google") return new GoogleProvider(desc, _httpFactory, _metadataCache);
        //if (driver == "github") return new GitHubProvider(desc, _httpFactory);
        //if (driver == "microsoft" || driver == "azuread") return new MicrosoftProvider(desc, _httpFactory, _metadataCache);

        if (driver == "keycloak") return InstanceProvider<KeycloakProvider>(desc);
        if (driver == "google") return InstanceProvider<GoogleProvider>(desc);
        if (driver == "github") return InstanceProvider<GitHubProvider>(desc);
        if (driver == "microsoft" || driver == "azuread") return InstanceProvider<MicrosoftProvider>(desc);

        // fallback: if descriptor.Algorithm is HS256 and SigningKey present, create a local-like provider
        if (!string.IsNullOrEmpty(desc.Algorithm) && desc.Algorithm.Equals("HS256", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(desc.SigningKey))
        {
            // create a simple HS256 validator using descriptor values
            return new DescriptorBasedHs256Provider(desc);
        }

        // default: throw or create a minimal adapter
        throw new InvalidOperationException($"No provider implementation for: {desc.Driver}");
    }

    T InstanceProvider<T>(SsoProviderDescriptor descriptor)
    {
        var instance = ActivatorUtilities.CreateInstance<T>(_serviceProvider, [descriptor]);
        return instance;
    }
}
