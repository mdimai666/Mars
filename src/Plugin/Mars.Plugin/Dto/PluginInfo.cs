using System.Reflection;

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

    public PluginInfo()
    {

    }

    public PluginInfo(Assembly assembly)
    {
        //[System.Runtime.CompilerServices.CompilationRelaxationsAttribute((Int32)8)]
        //[System.Runtime.CompilerServices.RuntimeCompatibilityAttribute(WrapNonExceptionThrows = True)]
        //[System.Diagnostics.DebuggableAttribute((System.Diagnostics.DebuggableAttribute + DebuggingModes)263)]
        //[Mars.Plugin.Abstractions.WebApplicationPluginAttribute(typeof(HelloPlugin1.HelloPlugin))]
        //[System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v6.0", FrameworkDisplayName = "")]
        //[System.Reflection.AssemblyCompanyAttribute("HelloPlugin1")]
        //[System.Reflection.AssemblyConfigurationAttribute("Debug")]
        //[System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
        //[System.Reflection.AssemblyInformationalVersionAttribute("1.0.0")]
        //[System.Reflection.AssemblyProductAttribute("HelloPlugin1")]
        //[System.Reflection.AssemblyTitleAttribute("HelloPlugin1")]

        AssemblyFullName = assembly.FullName!;
        AssemblyPath = assembly.Location;

        KeyName = Path.GetFileNameWithoutExtension(AssemblyPath);

        var _assembly = assembly.GetName();

        //this.Version = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0";
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
