using System.Reflection;
using System.Text.Json;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Sdk.Models;

namespace Mars.Plugin.Sdk;

internal static class PluginDescriptorWriter
{
    public static string Write(DirectoryInfo outDir, ProcessScriptSettings settings, string? iconFile = null)
    {
        var marsVersion = (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];

        var descriptor = new PluginPackageDescriptor
        {
            PackageType = PluginPackageDescriptor.MarsPluginPackageType,
            PackageId = settings.PackageId ?? settings.ProjectName,
            Version = settings.PackageVersion ?? "0.0.0",
            EntryAssembly = settings.ProjectName + ".dll",
            MarsVersion = marsVersion,
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            Title = string.IsNullOrWhiteSpace(settings.Title) ? null : settings.Title,
            Description = string.IsNullOrWhiteSpace(settings.Description) ? null : settings.Description,
            IconFile = iconFile
        };

        var path = Path.Combine(outDir.FullName, PluginPackageDescriptor.FileName);
        File.WriteAllText(path, JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Write descriptor: {path}");
        return path;
    }
}
