using Microsoft.Extensions.DependencyInjection;

namespace Mars.Forms.Front;

/// <summary>Подключение рендерера форм на WASM-фронте (админка, фронты модулей)</summary>
public static class MainFormsFront
{
    public static IServiceCollection AddMarsFormsFront(this IServiceCollection services)
    {
        services.AddScoped<IFormEditorLocator, FormEditorLocator>();
        services.AddScoped<IFormFieldTypeSettingsLocator, FormFieldTypeSettingsLocator>();
        return services;
    }

    /// <summary>Точка регистрации доменных редакторов потребителями</summary>
    public static IServiceProvider UseMarsFormsFront(this IServiceProvider services) => services;
}
