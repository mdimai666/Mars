using Microsoft.Extensions.DependencyInjection;

namespace Mars.Forms.Front;

/// <summary>Подключение рендерера форм на WASM-фронте (админка, фронты модулей)</summary>
public static class MainFormsFront
{
    public static IServiceCollection AddMarsFormsFront(this IServiceCollection services)
    {
        // реестры держим экземплярами (как локатор типов нод): зарегистрировать редактор или
        // панель можно откуда угодно — и из коллекции до сборки контейнера, и из сервис-провайдера
        // после, потому что записи собираются в момент запроса
        services.AddSingleton<IFormEditorLocator>(new FormEditorLocator());
        services.AddSingleton<IFormFieldTypeSettingsLocator>(new FormFieldTypeSettingsLocator());
        return services;
    }

    /// <summary>Точка регистрации доменных редакторов потребителями</summary>
    public static IServiceProvider UseMarsFormsFront(this IServiceProvider services) => services;
}
