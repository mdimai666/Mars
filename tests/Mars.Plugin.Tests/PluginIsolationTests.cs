using System.Reflection;
using System.Runtime.Loader;
using FluentAssertions;
using Mars.Plugin.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mars.Plugin.Tests;

/// <summary>
/// Изоляция плагинов: два плагина с разными версиями одной сторонней библиотеки
/// загружаются в разные AssemblyLoadContext и не конфликтуют.
/// </summary>
public class PluginIsolationTests : IDisposable
{
    private readonly DirectoryInfo _root;

    public PluginIsolationTests()
    {
        _root = Directory.CreateTempSubdirectory("mars-plugin-isolation-");
    }

    public void Dispose()
    {
        try { _root.Delete(recursive: true); }
        catch { /* временная папка */ }
    }

    [Fact]
    public void TwoPlugins_WithDifferentVersionsOfSameLibrary_LoadIsolated()
    {
        // две версии LibA с observable-разницей (readonly, чтобы не инлайнилось в плагин)
        var libA1 = Compile("LibA", """[assembly: System.Reflection.AssemblyVersion("1.0.0.0")] namespace LibA { public static class Info { public static readonly string Version = "1.0.0"; } }""", uniqueName: true);
        var libA2 = Compile("LibA", """[assembly: System.Reflection.AssemblyVersion("2.0.0.0")] namespace LibA { public static class Info { public static readonly string Version = "2.0.0"; } }""", uniqueName: true);

        // два плагина, каждый ссылается на свою версию LibA
        var plugin1 = CompilePlugin("Plugin1", libA1);
        var plugin2 = CompilePlugin("Plugin2", libA2);

        // папка плагина = entry-сборка + его версия LibA + Abstractions
        var dir1 = LayoutPlugin("plugin1", "Plugin1", plugin1, libA1);
        var dir2 = LayoutPlugin("plugin2", "Plugin2", plugin2, libA2);

        // каждый плагин — в свой изолированный контекст
        var asm1 = new PluginLoadContext(Path.Combine(dir1, "Plugin1.dll")).LoadFromAssemblyPath(Path.Combine(dir1, "Plugin1.dll"));
        var asm2 = new PluginLoadContext(Path.Combine(dir2, "Plugin2.dll")).LoadFromAssemblyPath(Path.Combine(dir2, "Plugin2.dll"));

        // изоляция
        var ctx1 = AssemblyLoadContext.GetLoadContext(asm1);
        var ctx2 = AssemblyLoadContext.GetLoadContext(asm2);
        Assert.True(!ReferenceEquals(ctx1, ctx2),
            $"same context: {ctx1?.Name} ({ctx1?.GetHashCode()}) vs {ctx2?.Name} ({ctx2?.GetHashCode()}); asm1={asm1.FullName} asm2={asm2.FullName}");

        // вызов метода резолвит LibA в контексте каждого плагина
        LibAVersionOf(asm1).Should().Be("1.0.0");
        LibAVersionOf(asm2).Should().Be("2.0.0");

        // каждый контекст несёт свою версию LibA
        var libA1Assembly = ctx1!.Assemblies.Single(a => a.GetName().Name == "LibA");
        var libA2Assembly = ctx2!.Assemblies.Single(a => a.GetName().Name == "LibA");
        libA1Assembly.Should().NotBeSameAs(libA2Assembly);
        libA1Assembly.GetName().Version.Should().Be(new Version(1, 0, 0, 0));
        libA2Assembly.GetName().Version.Should().Be(new Version(2, 0, 0, 0));
        libA1Assembly.GetType("LibA.Info").Should().NotBeSameAs(libA2Assembly.GetType("LibA.Info"));
    }

    static string LibAVersionOf(Assembly pluginAssembly)
    {
        var pluginType = pluginAssembly.GetTypes().Single(t => t.Name == "PluginEntry");
        var instance = Activator.CreateInstance(pluginType)!;
        var method = pluginType.GetMethod("LibAVersion")!;
        return (string)method.Invoke(instance, null)!;
    }

    string LayoutPlugin(string folderName, string entryAssemblyName, string pluginDll, string libADll)
    {
        var dir = _root.CreateSubdirectory(folderName);
        File.Copy(pluginDll, Path.Combine(dir.FullName, entryAssemblyName + ".dll"));
        File.Copy(libADll, Path.Combine(dir.FullName, "LibA.dll"));
        File.Copy(typeof(Mars.Plugin.Abstractions.MarsPlugin).Assembly.Location, Path.Combine(dir.FullName, "Mars.Plugin.Abstractions.dll"));
        return dir.FullName;
    }

    string CompilePlugin(string assemblyName, string libAPath)
    {
        var source = $$"""
            using Mars.Plugin.Abstractions;
            [assembly: MarsPlugin(typeof({{assemblyName}}.PluginEntry))]
            [assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
            namespace {{assemblyName}}
            {
                public class PluginEntry : Mars.Plugin.Abstractions.MarsPlugin
                {
                    public string LibAVersion() => LibA.Info.Version;
                }
            }
            """;
        return Compile(assemblyName, source, uniqueName: false,
            MetadataReference.CreateFromFile(typeof(Mars.Plugin.Abstractions.MarsPlugin).Assembly.Location),
            MetadataReference.CreateFromFile(libAPath));
    }

    string Compile(string assemblyName, string source, bool uniqueName, params MetadataReference[] extraReferences)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = GetTrustedPlatformReferences()
            .Concat(extraReferences);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var fileName = uniqueName ? $"{assemblyName}.{Guid.NewGuid():N}.dll" : $"{assemblyName}.dll";
        var path = Path.Combine(_root.FullName, fileName);
        using var stream = File.Create(path);
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        return path;
    }

    static IEnumerable<MetadataReference> GetTrustedPlatformReferences()
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
        return tpa.Split(Path.PathSeparator)
                  .Where(p => File.Exists(p))
                  .Select(p => MetadataReference.CreateFromFile(p));
    }
}
