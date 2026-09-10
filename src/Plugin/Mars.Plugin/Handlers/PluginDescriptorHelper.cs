using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Plugin.Abstractions;
using Mars.Plugin.Abstractions.Dto.Plugins;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Чтение и валидация дескриптора `mars-plugin.json` (общая логика детекта,
/// установки из zip/nuget и проверки совместимости).
/// </summary>
internal static class PluginDescriptorHelper
{
    /// <param name="descriptorFilePath">физический путь к файлу марс-plugin.json</param>
    internal static PluginPackageDescriptor? TryRead(string descriptorFilePath)
    {
        if (!File.Exists(descriptorFilePath)) return null;

        try
        {
            return JsonSerializer.Deserialize<PluginPackageDescriptor>(File.ReadAllText(descriptorFilePath));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Бросает <see cref="MarsValidationException"/>, если раскладка не валидна.</summary>
    internal static void Validate(PluginPackageDescriptor descriptor, string pluginDir)
    {
        if (descriptor.PackageType != PluginPackageDescriptor.MarsPluginPackageType)
            throw NewValidation($"Invalid package type '{descriptor.PackageType}' — expected '{PluginPackageDescriptor.MarsPluginPackageType}'.");

        if (string.IsNullOrWhiteSpace(descriptor.EntryAssembly))
            throw NewValidation("Descriptor has no EntryAssembly.");

        var entryPath = Path.Combine(pluginDir, descriptor.EntryAssembly);
        if (!File.Exists(entryPath))
            throw NewValidation($"Entry assembly '{descriptor.EntryAssembly}' not found in plugin folder.");

        ValidateCompatibility(descriptor);
    }

    /// <summary>MarsVersion — нижняя граница совместимости: хост не может быть старее.</summary>
    internal static void ValidateCompatibility(PluginPackageDescriptor descriptor)
    {
        var required = ParseVersionPrefix(descriptor.MarsVersion);
        if (required is null) return;

        var host = typeof(MarsPlugin).Assembly.GetName().Version;
        if (host is not null && required > host)
            throw NewValidation($"Plugin requires Mars >= {required}, but the host is {host}.");
    }

    /// <summary>«0.8.1-alpha.4» → 0.8.1; возвращает null, если распарсить не удалось.</summary>
    internal static Version? ParseVersionPrefix(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;
        var digits = version.Split('-')[0];
        return Version.TryParse(digits, out var parsed) ? parsed : null;
    }

    static MarsValidationException NewValidation(string message)
        => new(message, new Dictionary<string, string[]>());
}
