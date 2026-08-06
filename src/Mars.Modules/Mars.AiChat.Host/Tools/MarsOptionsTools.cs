using System.ComponentModel;
using System.Text.Json;
using Mars.Host.Shared.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: универсальное управление настройками сайта —
/// любой зарегистрированной опцией по имени класса (IOptionService).
/// </summary>
public class MarsOptionsTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    /// <summary>
    /// Запрещены к чтению: содержат секреты (пароль SMTP, API-ключи ИИ-сервисов)
    /// или технические данные, которые не должны попадать в модель.
    /// </summary>
    private static readonly HashSet<string> ReadDenied =
    [
        "SmtpSettingsModel",
        "AiChatOption",
        "FaviconOption",
        "FaviconOptionGenaratedValues",
    ];

    /// <summary>
    /// Дополнительно запрещены к записи: инфраструктурные настройки.
    /// </summary>
    private static readonly HashSet<string> WriteDenied = [.. ReadDenied, "PluginManagerSettingsOption"];

    private readonly IOptionService _optionService;

    public MarsOptionsTools(IOptionService optionService)
    {
        _optionService = optionService;
    }

    [Description("Получить список настроек сайта, доступных для управления: имена классов опций " +
                 "и признаки readable/writable. Вызывай перед чтением или изменением настроек.")]
    public string ListSiteOptions()
    {
        var list = _optionService.GetRegisteredOptionClasses()
            .Select(className => new
            {
                className,
                readable = !ReadDenied.Contains(className),
                writable = !WriteDenied.Contains(className),
            });

        return JsonSerializer.Serialize(list, SerializerOptions);
    }

    [Description("Прочитать текущее значение настройки сайта по имени класса опции. Возвращает JSON настройки. " +
                 "ОБЯЗАТЕЛЬНО прочитай настройку перед её изменением.")]
    public string GetSiteOption(
        [Description("Имя класса опции из результата list_site_options, например 'SEOOption'")] string className)
    {
        if (ReadDenied.Contains(className))
            return $"Настройка '{className}' защищена (содержит секретные данные) и недоступна агенту. " +
                   "Предложи пользователю изменить её вручную в админ-панели (Настройки).";

        try
        {
            var value = _optionService.GetOptionByClass(className);
            return JsonSerializer.Serialize(value, SerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Не удалось прочитать настройку '{className}': {ex.GetBaseException().Message}";
        }
    }

    [Description("Обновить настройку сайта по имени класса опции. Замещение полное: передай ПОЛНЫЙ новый JSON настройки — " +
                 "сначала прочитай текущее значение через get_site_option и измени в нём только нужные поля, " +
                 "сохранив точный регистр имён полей.")]
    public string UpdateSiteOption(
        [Description("Имя класса опции, например 'SEOOption'")] string className,
        [Description("Полный новый JSON настройки")] string json)
    {
        if (WriteDenied.Contains(className))
            return $"Настройка '{className}' защищена и не может быть изменена агентом. " +
                   "Предложи пользователю изменить её вручную в админ-панели (Настройки).";

        try
        {
            _optionService.SetOptionByClass(className, json);
        }
        catch (Exception ex)
        {
            return $"Не удалось сохранить настройку '{className}': {ex.GetBaseException().Message}. " +
                   "Проверь имя класса и корректность JSON (регистр имён полей должен совпадать с прочитанным значением).";
        }

        try
        {
            var value = _optionService.GetOptionByClass(className);
            return $"Настройка '{className}' сохранена. Текущее значение: " + JsonSerializer.Serialize(value, SerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Настройка '{className}' сохранена, но проверить результат не удалось: {ex.GetBaseException().Message}";
        }
    }
}
