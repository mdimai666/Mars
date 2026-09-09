using Microsoft.Extensions.DependencyInjection;

namespace Mars.Forms.Abstractions.Services;

internal class FormDataProviderLocator(IServiceCollection serviceCollection) : IFormDataProviderLocator
{
    /// <summary>Ключи keyed-регистраций провайдеров, снятые один раз с коллекции сервисов</summary>
    readonly HashSet<string> _keys = new(serviceCollection.Where(x => x.IsKeyedService
                                                                      && x.ServiceType == typeof(IFormDataProvider)
                                                                      && x.ServiceKey is string)
                                                          .Select(x => (string)x.ServiceKey!),
                                         StringComparer.Ordinal);

    public IReadOnlyCollection<string> OwnerModels => _keys;

    public IFormDataProvider? GetProvider(string ownerModel, IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(ownerModel)) return null;

        foreach (var scope in FormOwnerScopes.Of(ownerModel))
        {
            if (_keys.Contains(scope))
                return serviceProvider.GetKeyedService<IFormDataProvider>(scope);
        }

        return null;
    }
}
