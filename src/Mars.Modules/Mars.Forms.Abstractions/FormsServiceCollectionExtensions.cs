using Mars.Forms.Abstractions.Services;
using Mars.Forms.Abstractions.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mars.Forms.Abstractions;

/// <summary>Подключение общего слоя форм: нормализатор раскладки, локатор провайдеров, реестр правил, валидатор</summary>
public static class FormsServiceCollectionExtensions
{
    public static IServiceCollection AddMarsForms(this IServiceCollection services)
    {
        // локатор провайдеров перечисляет keyed-регистрации по коллекции сервисов
        // (в Mars.Server она уже зарегистрирована; здесь — чтобы модуль работал автономно)
        services.TryAddSingleton<IServiceCollection>(services);

        services.TryAddSingleton<IFormDefinitionNormalizer, FormDefinitionNormalizer>();
        services.TryAddSingleton<IFormDataProviderLocator, FormDataProviderLocator>();
        services.TryAddSingleton<IFormValidator, FormValidator>();
        services.TryAddSingleton<IFormRuleRegistry>(_ =>
        {
            var registry = new FormRuleRegistry();
            BuiltInFormRules.RegisterAll(registry);
            return registry;
        });

        return services;
    }
}
