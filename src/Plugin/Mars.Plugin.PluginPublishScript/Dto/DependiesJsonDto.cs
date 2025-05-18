namespace Mars.Plugin.PluginPublishScript.Dto;

public class RuntimeTargetJsonDto
{
    public string name { get; set; } = default!;
    public string signature { get; set; } = default!;
}

public class DependencyJsonDto
{
    public Dictionary<string, string> dependencies { get; set; } = default!;
    public Dictionary<string, AssemblyVersionInfoJsonDto> runtime { get; set; } = default!;
    public Dictionary<string, ResourceInfoJsonDto> resources { get; set; } = default!;
}

public class AssemblyVersionInfoJsonDto
{
    public string assemblyVersion { get; set; } = default!;
    public string fileVersion { get; set; } = default!;
}

public class ResourceInfoJsonDto
{
    public string locale { get; set; } = default!;
}

public class TargetFrameworkJsonDto : Dictionary<string, DependencyJsonDto>
{

}

public class LibraryJsonDto
{
    public string type { get; set; } = default!;
    public bool serviceable { get; set; } = default!;
    public string sha512 { get; set; } = default!;
    public string path { get; set; } = default!;
    public string hashPath { get; set; } = default!;
}

/// <summary>
/// Класс для десериализации JSON-файла зависимостей (например, `project.assets.json`).
/// </summary>
public class DependenciesJsonDto
{
    public RuntimeTargetJsonDto runtimeTarget { get; set; } = default!;
    public Dictionary<string, string> compilationOptions { get; set; } = default!;
    public Dictionary<string, TargetFrameworkJsonDto> targets { get; set; } = default!;
    public Dictionary<string, LibraryJsonDto> libraries { get; set; } = default!;

    public TargetFrameworkJsonDto Packages => targets[runtimeTarget.name];
}
