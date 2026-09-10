using Mars.Forms.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Forms.Front;

/// <summary>Подключение рендерера форм на WASM-фронте (админка, фронты модулей)</summary>
public static class MainFormsFront
{
    public static IServiceCollection AddMarsFormsFront(this IServiceCollection services)
    {
        // реестры неизменяемы после сборки контейнера и делятся на всё приложение
        services.AddSingleton<IFormEditorLocator, FormEditorLocator>();
        services.AddSingleton<IFormFieldTypeSettingsLocator, FormFieldTypeSettingsLocator>();
        return services;
    }

    /// <summary>
    /// Регистрация редактора значения (админка, модуль, плагин) — до сборки контейнера.
    /// Название делает редактор предлагаемым в выборе редактора поля.
    /// </summary>
    public static IServiceCollection AddFormEditor(this IServiceCollection services, string editorKey, Type component,
                                                   bool multiple, string? title, params FormFieldType[] fieldTypes)
        => services.AddSingleton(new FormEditorRegistration(editorKey, component, multiple, title, fieldTypes));

    /// <summary>
    /// Регистрация панели доменных настроек поля (админка, модуль, плагин) — до сборки контейнера.
    /// Без типов — панель показывается на всех полях скоупа.
    /// </summary>
    public static IServiceCollection AddFormFieldSettingsPanel(this IServiceCollection services, string scope,
                                                              Type component, params FormFieldType[] fieldTypes)
        => services.AddSingleton(new FormFieldTypeSettingsRegistration(scope, component, fieldTypes));

    /// <summary>Точка регистрации доменных редакторов потребителями</summary>
    public static IServiceProvider UseMarsFormsFront(this IServiceProvider services) => services;
}
