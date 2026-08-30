using System.ComponentModel.DataAnnotations;

namespace Mars.Plugin.Contracts.Options;

[Display(Name = "Настройки менеджера плагинов")]
public class PluginManagerSettingsOption
{
    [Display(Name = "Разрешить загрузку zip-файлов плагинов вручную")]
    public bool AllowUploadZipManually { get; set; } = true;
}
