using System.Reflection;
using System.Text.Json;
using Mars.Plugin.Sdk.Models;

namespace Mars.Plugin.Sdk;

internal static class PluginDescriptorWriter
{
    public const string DescriptorFileName = "mars-plugin.json";

    public static string Write(DirectoryInfo outDir, ProcessScriptSettings settings)
    {
        var marsVersion = (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];

        var descriptor = new MarsPluginDescriptor
        {
            PackageType = "MarsPlugin",
            PackageId = settings.PackageId ?? settings.ProjectName,
            Version = settings.PackageVersion ?? "0.0.0",
            EntryAssembly = settings.ProjectName + ".dll",
            MarsVersion = marsVersion,
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        var path = Path.Combine(outDir.FullName, DescriptorFileName);
        File.WriteAllText(path, JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Write descriptor: {path}");
        return path;
    }
}

internal class MarsPluginDescriptor
{
    public string PackageType { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string EntryAssembly { get; set; } = string.Empty;

    /// <summary>
    /// Версия Марса, инструментом которой собран пакет (нижняя граница совместимости).
    /// </summary>
    public string MarsVersion { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
}
