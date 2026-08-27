using System.ComponentModel;
using System.Text.Json;
using Mars.Options.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента для работы с настройками сайта (SysOptions).
/// </summary>
public class MarsSiteTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly IOptionService _optionService;

    public MarsSiteTools(IOptionService optionService)
    {
        _optionService = optionService;
    }

    [Description("Получить текущие настройки сайта: имя сайта, описание, адрес (URL), email администратора.")]
    public string GetSiteSettings()
    {
        var opt = _optionService.SysOption;

        return JsonSerializer.Serialize(new
        {
            opt.SiteName,
            opt.SiteDescription,
            opt.SiteUrl,
            opt.AdminEmail,
        }, SerializerOptions);
    }

    [Description("Изменить настройки сайта. Передай только те поля, которые нужно изменить — пустые строки не меняют значения. " +
                 "Изменяемые поля: имя сайта (siteName), описание сайта (siteDescription), email администратора (adminEmail).")]
    public string UpdateSiteSettings(
        [Description("Новое имя сайта. Пустая строка — не менять.")] string siteName = "",
        [Description("Новое описание сайта. Пустая строка — не менять.")] string siteDescription = "",
        [Description("Новый email администратора. Пустая строка — не менять.")] string adminEmail = "")
    {
        var opt = _optionService.SysOption;

        if (!string.IsNullOrWhiteSpace(siteName)) opt.SiteName = siteName.Trim();
        if (!string.IsNullOrWhiteSpace(siteDescription)) opt.SiteDescription = siteDescription.Trim();
        if (!string.IsNullOrWhiteSpace(adminEmail)) opt.AdminEmail = adminEmail.Trim();

        _optionService.SaveOption(opt);

        return "Настройки сайта сохранены. Текущие значения: " + GetSiteSettings();
    }
}
