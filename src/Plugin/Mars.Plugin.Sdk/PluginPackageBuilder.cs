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

        // Файлы лицензии/ридами, на которые ссылается nuspec, обязаны лежать в пакете.
        var readmeFile = ResolveMetadataFile(settings, settings.ReadmeFile);
        var licenseFile = ResolveMetadataFile(settings, settings.LicenseFile);

        var nuspec = BuildNuspec(settings, data, licenseFile?.entryName, readmeFile?.entryName);

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

        if (readmeFile is not null) zip.CreateEntryFromFile(readmeFile.Value.sourcePath, readmeFile.Value.entryName, CompressionLevel.Optimal);
        if (licenseFile is not null) zip.CreateEntryFromFile(licenseFile.Value.sourcePath, licenseFile.Value.entryName, CompressionLevel.Optimal);

        return nupkgPath;
    }

    /// <summary>
    /// Файл метаданных (ридами/лицензия) из свойства csproj: относительный путь от
    /// проекта, в пакете и в элементе нуспека — один и тот же путь со слешами.
    /// </summary>
    static (string entryName, string sourcePath)? ResolveMetadataFile(ProcessScriptSettings settings, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var sourcePath = Path.GetFullPath(Path.Combine(settings.ProjectDir, relativePath));
        if (!File.Exists(sourcePath))
            throw new InvalidOperationException($"Файл метаданных пакета '{relativePath}' не найден: '{sourcePath}'.");

        var entryName = relativePath.Replace('\\', '/').TrimStart('/');
        return (entryName, sourcePath);
    }

    static void AddEntry(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    static string BuildNuspec(ProcessScriptSettings settings, PreparePublishData data, string? licenseFileEntry, string? readmeEntry)
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
        var projectUrl = string.IsNullOrWhiteSpace(settings.ProjectUrl) ? "" : $"\n    <projectUrl>{Escape(settings.ProjectUrl)}</projectUrl>";
        var copyright = string.IsNullOrWhiteSpace(settings.Copyright) ? "" : $"\n    <copyright>{Escape(settings.Copyright)}</copyright>";
        var releaseNotes = string.IsNullOrWhiteSpace(settings.ReleaseNotes) ? "" : $"\n    <releaseNotes>{Escape(settings.ReleaseNotes)}</releaseNotes>";

        // LicenseExpression и LicenseFile взаимоисключающие (как в стандартном паке);
        // при обоих берётся expression.
        var license = !string.IsNullOrWhiteSpace(settings.LicenseExpression)
            ? $"\n    <license type=\"expression\">{Escape(settings.LicenseExpression)}</license>"
            : licenseFileEntry is not null ? $"\n    <license type=\"file\">{Escape(licenseFileEntry)}</license>" : "";

        var readme = readmeEntry is not null ? $"\n    <readme>{Escape(readmeEntry)}</readme>" : "";

        var repository = "";
        if (!string.IsNullOrWhiteSpace(settings.RepositoryUrl))
        {
            var attrs = new List<string> { $"url=\"{Escape(settings.RepositoryUrl)}\"" };
            if (!string.IsNullOrWhiteSpace(settings.RepositoryType)) attrs.Add($"type=\"{Escape(settings.RepositoryType)}\"");
            if (!string.IsNullOrWhiteSpace(settings.RepositoryBranch)) attrs.Add($"branch=\"{Escape(settings.RepositoryBranch)}\"");
            if (!string.IsNullOrWhiteSpace(settings.RepositoryCommit)) attrs.Add($"commit=\"{Escape(settings.RepositoryCommit)}\"");
            repository = $"\n    <repository {string.Join(" ", attrs)} />";
        }

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{Escape(settings.PackageId)}</id>
                <version>{Escape(settings.PackageVersion)}</version>{title}
                <authors>{Escape(settings.Authors) ?? Escape(settings.PackageId)}</authors>
                <description>{Escape(settings.Description) ?? "Mars plugin"}</description>{tags}{icon}{projectUrl}{copyright}{license}{readme}{releaseNotes}{repository}
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
