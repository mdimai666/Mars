using System.Diagnostics;
using System.Text.Json;
using Mars.Plugin.Front.Abstractions;

namespace Mars.Plugin.Sdk.Models;

public class ProjectDependencies
{
    public RuntimeTarget RuntimeTarget { get; set; }
    public Dictionary<string, TargetFramework> Targets { get; set; } = default!;

    public Dictionary<string, Library> Libraries { get; set; } = default!;

    public TargetFramework Packages => Targets[RuntimeTarget.Name];

    internal ProjectDependencies(string releaseArtifactsDepsjsonFile)
    {
        DependenciesJsonDto json = JsonSerializer.Deserialize<DependenciesJsonDto>(File.ReadAllText(releaseArtifactsDepsjsonFile))!;

        RuntimeTarget = new(json.runtimeTarget);

        Targets = new(json.targets.Count);
        foreach (var (tkey, target) in json.targets)
        {
            var tf = new TargetFramework();

            foreach (var (dkey, dep) in target)
            {
                var d = new Dependency(dkey, dep);
                tf.Add(d.Name, d);
            }

            Targets[tkey] = tf;
        }

        Libraries = json.libraries.ToDictionary(s => s.Key.Split('/')[0], s => new Library(s.Key, s.Value));

    }
}

public class RuntimeTarget(RuntimeTargetJsonDto runtimeTarget)
{
    public string Name { get; set; } = runtimeTarget.name;
}

[DebuggerDisplay("{Name}/{Version}")]
public class Dependency
{
    public string Name { get; set; }
    public string Version { get; set; }

    public Dictionary<string, DependencyItem> Dependencies { get; set; }
    public Dictionary<string, AssemblyVersionInfoJsonDto> Runtime { get; set; }
    public Dictionary<string, ResourceInfo> Resources { get; set; }

    public Dependency(string key, DependencyJsonDto dependencyJsonDto)
    {
        var sp = key.Split('/', 2);
        Name = sp[0];
        Version = sp[1];

        Runtime = dependencyJsonDto.runtime?.ToDictionary(s => Path.GetFileName(s.Key), s => s.Value) ?? [];

        Dependencies = dependencyJsonDto.dependencies
                            ?.ToDictionary(s => s.Key, s => new DependencyItem() { Name = s.Key, Version = s.Value })
                            ?? [];

        Resources = dependencyJsonDto.resources?.ToDictionary(s => s.Key, s => new ResourceInfo { Locale = s.Value.locale }) ?? [];
    }
}

[DebuggerDisplay("{Name}/{Version}")]
public class DependencyItem
{
    public required string Name { get; set; }
    public required string Version { get; set; }
}

[DebuggerDisplay("{Locale}")]
public class ResourceInfo
{
    public required string Locale { get; set; }
}

public class TargetFramework : Dictionary<string, Dependency>
{
}

[DebuggerDisplay("{Name}/{Version}")]
public class Library
{
    public LibraryType Type { get; set; }

    public string Name { get; set; }
    public string Version { get; set; }

    public Library(string key, LibraryJsonDto libraryJsonDto)
    {
        Type = libraryJsonDto.type switch
        {
            "framework" => LibraryType.Framework,
            "package" => LibraryType.Package,
            "project" => LibraryType.Project,
            "reference" => LibraryType.Reference,
            _ => throw new NotImplementedException($"LibraryJsonDto type '{libraryJsonDto.type}' is not implement")
        };

        var sp = key.Split('/', 2);
        Name = sp[0];
        Version = sp[1];
    }
}

public enum LibraryType
{
    Package,
    Project,
    Framework,
    Reference
}
