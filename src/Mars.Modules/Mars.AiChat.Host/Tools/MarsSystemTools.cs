using System.ComponentModel;
using System.Text.Json;
using Mars.Host.Shared.Services;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: информация о системе и приложении Mars
/// (версия, ОС, окружение, Docker/pm2, аптайм, память) через IMarsSystemService.
/// </summary>
public class MarsSystemTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly IMarsSystemService _systemService;

    public MarsSystemTools(IMarsSystemService systemService)
    {
        _systemService = systemService;
    }

    [Description("Получить информацию о системе и приложении Mars: версия приложения, git-коммит, ОС и архитектура, " +
                 "окружение (Environment), запущено ли приложение в Docker (IsRunningInDocker) или под pm2 (IsPM2), " +
                 "часовые поясы, аптайм приложения и использование памяти. " +
                 "Вызывай, когда пользователь спрашивает о версии, окружении или способе запуска.")]
    public string GetSystemInfo()
    {
        var info = _systemService.AboutSystem()
            .Where(kv => !string.IsNullOrEmpty(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        info["AppUptime"] = _systemService.AppUptime();
        info["MemoryUsage"] = _systemService.MemoryUsage();

        return JsonSerializer.Serialize(info, SerializerOptions);
    }
}
