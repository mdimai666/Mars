using System.Reflection;
using Mars.Plugin.Contracts.Plugins;

namespace Mars.Plugin.Dto;

public class PluginInfo
{
    public string AssemblyFullName { get; set; } = default!;
    public string AssemblyPath { get; set; } = default!;

    public string PackageId { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string KeyName { get; set; } = default!;
    public string Description { get; set; } = default!;

    internal Assembly Assembly { get; set; } = default!;
    public string[] PackageTags { get; set; } = [];

    public string? ManifestFile { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? PackageIcon { get; set; }

    /// <summary>Откуда плагин: конфигурация инстанса (Locked) или установлен из zip/nuget.</summary>
    public PluginSource Source { get; set; } = PluginSource.Unknown;
    public bool Locked => Source == PluginSource.Config;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset InstalledAt { get; set; } = DateTimeOffset.MinValue;

    public PluginInfo()
    {

    }

    public PluginInfo(Assembly assembly)
    {
        AssemblyFullName = assembly.FullName!;
        AssemblyPath = assembly.Location;

        KeyName = Path.GetFileNameWithoutExtension(AssemblyPath);

        var _assembly = assembly.GetName();

        Version = _assembly.Version.ToString() ?? "0";
        Title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? _assembly.Name!;
        Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";

        var meta = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        PackageId = meta.FirstOrDefault(s => s.Key == "PackageId")?.Value ?? KeyName;
        PackageTags = meta.FirstOrDefault(s => s.Key == "PackageTags")?.Value?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];

        RepositoryUrl = meta.FirstOrDefault(s => s.Key == "RepositoryUrl")?.Value;
        PackageIcon = meta.FirstOrDefault(s => s.Key == "PackageIcon")?.Value;

        Assembly = assembly;
    }
}
