using System.Reflection;
using System.Reflection.Metadata;

namespace Mars.Plugin;

public class PluginInfo
{
    public string AssemblyFullName { get; set; } = default!;
    public string AssemblyPath { get; set; } = default!;
    
    public string Version { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;

    internal Assembly Assembly { get; set; } = default!;

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

        this.AssemblyFullName = assembly.FullName!;
        this.AssemblyPath = assembly.Location;

        var _assembly = assembly.GetName();

        //this.Version = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0";
        this.Version = _assembly.Version.ToString() ?? "0";
        this.Title = assembly.GetCustomAttribute<System.Reflection.AssemblyTitleAttribute>()?.Title ?? _assembly.Name!;
        this.Description = assembly.GetCustomAttribute<System.Reflection.AssemblyDescriptionAttribute>()?.Description?? "";

        this.Assembly = assembly;
    }
}
