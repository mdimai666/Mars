using Mars.Host.Shared.SSO.Dto;
using Mars.Host.Shared.SSO.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SSO.Providers;

/// <summary>
/// DynamicSsoProvider factory that creates provider instances from descriptors
/// </summary>
public class DynamicSsoProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DynamicSsoProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ISsoProvider Create(SsoProviderDescriptor desc)
    {
        var name = desc.Name?.ToLowerInvariant();
        var driver = desc.Driver;

        if (driver == "keycloak") return InstanceProvider<KeycloakProvider>(desc);
        if (driver == "mars") return InstanceProvider<ExternalMarsProvider>(desc);
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
