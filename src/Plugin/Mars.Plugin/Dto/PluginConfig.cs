namespace Mars.Plugin.Dto;

/// <summary>
/// IConfiguration Manager Bind
/// </summary>
public class PluginConfig
{
    public string? ContentRootPath { get; set; }
    public string AssemblyPath { get; set; } = default!;
}
