using System.IO.Compression;
using System.Security;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Sdk.Models;

namespace Mars.Plugin.Sdk;

internal static class PluginPackageBuilder
{
    public static string BuildZip(DirectoryInfo outDir, ProcessScriptSettings settings)
    {
        var zipName = $"{settings.PackageId ?? settings.ProjectName}-{settings.PackageVersion ?? "0.0.0"}.zip";
        var zipPath = Path.Combine(outDir.Parent!.FullName, zipName);
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(outDir.FullName, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        return zipPath;
    }

    /// <summary>
    /// Классический лейаут: собственные сборки в lib/, фронт-ассеты и дескриптор в mars/.
    /// Сторонние зависимости не кладутся в пакет — они объявлены в nuspec, и Марс
    /// зарезолвит их при установке, скопировав только отсутствующие у него.
    /// </summary>
    public static string BuildNuget(DirectoryInfo outDir, ProcessScriptSettings settings, PreparePublishData data, HashSet<string> selfDlls, string descriptorPath)
    {
        var nupkgName = $"{settings.PackageId}.{settings.PackageVersion}.nupkg";
        var nupkgPath = Path.Combine(outDir.Parent!.FullName, nupkgName);
        if (File.Exists(nupkgPath)) File.Delete(nupkgPath);

        var nuspec = BuildNuspec(settings, data);

        using var zip = ZipFile.Open(nupkgPath, ZipArchiveMode.Create);
        AddEntry(zip, $"{settings.PackageId}.nuspec", nuspec);

        foreach (var relPath in selfDlls)
        {
            var file = new FileInfo(Path.Combine(outDir.FullName, relPath.Replace('/', Path.DirectorySeparatorChar)));
            if (file.Exists)
                zip.CreateEntryFromFile(file.FullName, $"lib/net10.0/{relPath}", CompressionLevel.Optimal);
        }

        // Файлы рантайма вне managed-сборок кладём под lib/<tfm>/: инсталлер извлекает всё
        // содержимое lib/ без фильтра по типу, поэтому они лягут ровно туда, где их ждёт рантайм.
        //  - <ProjectName>.staticwebassets.endpoints.json — источник фронт-манифеста
        //    (PluginManifestProvider читает его из корня установленного плагина);
        //  - libs/** — нативные библиотеки (конвенция: SetDllDirectory ищет libs/ рядом со сборкой).
        var endpointsJson = Path.Combine(outDir.FullName, settings.ProjectName + ".staticwebassets.endpoints.json");
        if (File.Exists(endpointsJson))
            zip.CreateEntryFromFile(endpointsJson, $"lib/net10.0/{Path.GetFileName(endpointsJson)}", CompressionLevel.Optimal);

        var libsDir = new DirectoryInfo(Path.Combine(outDir.FullName, "libs"));
        if (libsDir.Exists)
            foreach (var file in libsDir.GetFiles("*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(libsDir.FullName, file.FullName).Replace('\\', '/');
                zip.CreateEntryFromFile(file.FullName, $"lib/net10.0/libs/{rel}", CompressionLevel.Optimal);
            }

        var wwwroot = new DirectoryInfo(Path.Combine(outDir.FullName, "wwwroot"));
        if (wwwroot.Exists)
            foreach (var file in wwwroot.GetFiles("*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(wwwroot.FullName, file.FullName).Replace('\\', '/');
                zip.CreateEntryFromFile(file.FullName, $"mars/front/{rel}", CompressionLevel.Optimal);
            }

        zip.CreateEntryFromFile(descriptorPath, $"mars/{PluginPackageDescriptor.FileName}", CompressionLevel.Optimal);

        if (!string.IsNullOrWhiteSpace(settings.Icon))
        {
            var iconCandidates = new[]
            {
                Path.Combine(outDir.FullName, settings.Icon),
                Path.Combine(outDir.FullName, "wwwroot", settings.Icon)
            };
            var icon = iconCandidates.FirstOrDefault(File.Exists);
            if (icon != null) zip.CreateEntryFromFile(icon, settings.Icon!, CompressionLevel.Optimal);
        }

        return nupkgPath;
    }

    static void AddEntry(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    static string BuildNuspec(ProcessScriptSettings settings, PreparePublishData data)
    {
        static string? Escape(string? v) => string.IsNullOrWhiteSpace(v) ? null : SecurityElement.Escape(v);

        var deps = data.ProjectDependencies.Libraries.Values
            .Where(l => l.Type == LibraryType.Package)
            .Where(l => l.Name != PreparePublishData.MarsSdkPackageId)
            .Where(l => !l.Name.Equals("Microsoft.AspNetCore.Components.WebAssembly.DevServer", StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l.Name)
            .Select(l => $"""      <dependency id="{Escape(l.Name)}" version="{Escape(l.Version)}" />""");

        var dependenciesXml = string.Join("\n", deps);
        var icon = string.IsNullOrWhiteSpace(settings.Icon) ? "" : $"\n    <icon>{Escape(settings.Icon)}</icon>";
        var tags = string.IsNullOrWhiteSpace(settings.Tags) ? "" : $"\n    <tags>{Escape(settings.Tags)}</tags>";
        var title = string.IsNullOrWhiteSpace(settings.Title) ? "" : $"\n    <title>{Escape(settings.Title)}</title>";

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{Escape(settings.PackageId)}</id>
                <version>{Escape(settings.PackageVersion)}</version>{title}
                <authors>{Escape(settings.Authors) ?? Escape(settings.PackageId)}</authors>
                <description>{Escape(settings.Description) ?? "Mars plugin"}</description>{tags}{icon}
                <packageTypes>
                  <packageType name="MarsPlugin" />
                </packageTypes>
                <dependencies>
                  <group targetFramework="net10.0">
            {dependenciesXml}
                  </group>
                </dependencies>
              </metadata>
            </package>
            """;
    }
}
