using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mars.Integration.Tests.Controllers.Plugins;

public static class PluginAssemblyGenerator
{
    public static byte[] GeneratePluginAssembly(string assemblyName = "DynamicPluginAssembly",
                                                string? title = null,
                                                string? version = "0.0.0",
                                                Dictionary<string, string>? metadata = null)
    {
        var source = GenerateSource(assemblyName, title, version, metadata);

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
            throw new Exception("Compilation failed:\n" + errors);
        }

        return ms.ToArray();
    }

    private static string GenerateSource(string assemblyName = "DynamicPluginAssembly",
                                        string? title = null,
                                        string? version = null,
                                        Dictionary<string, string>? metadata = null)
    {
        var attrs = new StringBuilder();

        if (!string.IsNullOrEmpty(title)) attrs.AppendLine($"[assembly: System.Reflection.AssemblyTitle(\"{Escape(title ?? assemblyName)}\")]");

        if (Version.TryParse(version, out _))
        {
            attrs.AppendLine($"[assembly: System.Reflection.AssemblyVersion(\"{version}\")]");
            attrs.AppendLine($"[assembly: System.Reflection.AssemblyFileVersion(\"{version}\")]");
        }

        if (!string.IsNullOrEmpty(version)) attrs.AppendLine($"""[assembly: System.Reflection.AssemblyInformationalVersion("{version}")]""");

        attrs.AppendLine(string.Join("\n", metadata?.Select(kvp =>
            $"[assembly: System.Reflection.AssemblyMetadata(\"{Escape(kvp.Key)}\", \"{Escape(kvp.Value)}\")]") ?? []));

        return $$"""
            {{attrs.ToString()}}
            namespace {{assemblyName}}
            {
                public static class DummyClass { }
            }
            """;
    }

    private static string? Escape(string? value)
    {
        return value?.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public static Stream GeneratePluginAssemblyAndZip(string assemblyName = "DynamicPluginAssembly",
                                                string? title = null,
                                                string? version = "1.0.0",
                                                Dictionary<string, string>? metadata = null)
        => ZipHelper.ZipFiles(new()
        {
            ["plugin.dll"] = GeneratePluginAssembly(assemblyName, title, version, metadata)
        });
}
