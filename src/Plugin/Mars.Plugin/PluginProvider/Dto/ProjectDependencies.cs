using System.Diagnostics;
using System.Text.Json;

namespace Mars.Plugin.PluginProvider.Dto;

public class ProjectDependencies
{
    public RuntimeTarget RuntimeTarget { get; set; }
    public Dictionary<string, string> CompilationOptions { get; set; } = default!;
    public Dictionary<string, TargetFramework> Targets { get; set; } = default!;

    public Dictionary<string, Library> Libraries { get; set; } = default!;

    public TargetFramework Packages => Targets[RuntimeTarget.Name];

    internal ProjectDependencies(string releaseArtifactsDepsjsonFile)
    {
        DependenciesJsonDto json = JsonSerializer.Deserialize<DependenciesJsonDto>(File.ReadAllText(releaseArtifactsDepsjsonFile))!;

        RuntimeTarget = new(json.runtimeTarget);
        CompilationOptions = new Dictionary<string, string>(json.compilationOptions);

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
    public string Signature { get; set; } = runtimeTarget.signature;
}

[DebuggerDisplay("{Name}/{Version}")]
public class Dependency
{
    public string Name { get; set; }
    public string Version { get; set; }

    /// <summary>
    /// Pakage.Name/0.0.1ver
    /// </summary>
    public string KeyPath { get; set; }

    public Dictionary<string, DependencyItem> Dependencies { get; set; }
    public Dictionary<string, AssemblyVersionInfo> Runtime { get; set; }
    public Dictionary<string, ResourceInfo> Resources { get; set; }

    public Dependency(string key, DependencyJsonDto dependencyJsonDto)
    {
        var sp = key.Split('/', 2);
        Name = sp[0];
        Version = sp[1];
        KeyPath = key;

        Runtime = dependencyJsonDto.runtime?.ToDictionary(s => Path.GetFileName(s.Key), s => new AssemblyVersionInfo(key, s.Value)) ?? [];

        Dependencies = dependencyJsonDto.dependencies
                            ?.ToDictionary(s => s.Key, s => new DependencyItem() { Name = s.Key, Version = s.Value })
                            ?? [];

        Resources = dependencyJsonDto.resources?.ToDictionary(s => s.Key, s => new ResourceInfo
        {
            Locale = s.Value.locale,
            DllFilePath = s.Key,
            DllFileName = Path.GetFileName(s.Key),
        }) ?? [];
    }
}

[DebuggerDisplay("{Name}/{Version}")]
public class DependencyItem
{
    public required string Name { get; set; }
    public required string Version { get; set; }
}

[DebuggerDisplay("{DllFilePath}/{AssemblyVersion}")]
public class AssemblyVersionInfo
{
    public string? AssemblyVersion { get; set; }
    public string? FileVersion { get; set; }
    public string DllFilePath { get; set; } = default!;
    public string DllFileName { get; set; } = default!;

    public AssemblyVersionInfo(string key, AssemblyVersionInfoJsonDto assemblyVersionInfoJsonDto)
    {
        DllFilePath = key;
        DllFileName = Path.GetFileName(key);
        AssemblyVersion = assemblyVersionInfoJsonDto.assemblyVersion;
        FileVersion = assemblyVersionInfoJsonDto.fileVersion;
    }
}

[DebuggerDisplay("{Locale}/{DllFilePath}")]
public class ResourceInfo
{
    public required string Locale { get; set; }
    public required string DllFilePath { get; set; }
    public required string DllFileName { get; set; }
}

public class TargetFramework : Dictionary<string, Dependency>
{
    //public TargetFramework(IEnumerable<KeyValuePair<string,Dependency>> collection) :base(collection)
    //{

    //}
}

[DebuggerDisplay("{KeyPath}")]
public class Library
{
    public LibraryType Type { get; set; }
    public bool Serviceable { get; set; }
    public string Sha512 { get; set; }
    public string PackageKeyPath { get; set; }
    public string HashKeyPath { get; set; }

    public string Name { get; set; }
    public string Version { get; set; }

    /// <summary>
    /// Pakage.Name/0.0.1ver
    /// </summary>
    public string KeyPath { get; set; }

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

        Serviceable = libraryJsonDto.serviceable;
        Sha512 = libraryJsonDto.sha512;
        PackageKeyPath = libraryJsonDto.path;
        HashKeyPath = libraryJsonDto.hashPath;

        var sp = key.Split('/', 2);
        Name = sp[0];
        Version = sp[1];
        KeyPath = key;
    }
}

public enum LibraryType
{
    Package,
    Project,
    Framework,
    Reference
}
