using System.IO.Compression;
using Mars.Core.Exceptions;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Dto;
using Mars.Plugin.Front.Abstractions;
using Mars.Plugin.Services;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Mars.Plugin.Handlers;

/// <summary>
/// Установка плагина из nuget-фида: скачивает пакет, резолвит транзитивные
/// зависимости и раскладывает в `plugins/&lt;PackageId&gt;/` только то, чего нет в Марсе.
/// </summary>
internal class PluginNugetInstaller
{
    private const string DescriptorEntry = "mars/" + PluginPackageDescriptor.FileName;
    private const string FrontEntryPrefix = "mars/front/";

    private readonly IFileStorage _fileStorage;
    private readonly ILogger _logger;
    private readonly string _marsDepsJsonPath;
    private readonly PluginRegistry _registry;

    public PluginNugetInstaller(IFileStorage fileStorage, ILogger logger, PluginRegistry? registry = null, string? marsDepsJsonPath = null)
    {
        _fileStorage = fileStorage;
        _logger = logger;
        _registry = registry ?? new PluginRegistry(fileStorage);
        _marsDepsJsonPath = marsDepsJsonPath ?? Path.Combine(AppContext.BaseDirectory, "Mars.deps.json");
    }

    public async Task<PluginInstallResult> InstallAsync(string packageId, string? version,
                                                        IReadOnlyCollection<string> sources, CancellationToken cancellationToken)
    {
        if (sources.Count == 0)
            throw NewValidation("No nuget sources configured.");

        var repos = sources.Select(s => Repository.CreateSource(Repository.Provider.GetCoreV3(), new PackageSource(s))).ToList();
        var requestedVersion = ParseRequestedVersion(version);

        var (resolvedVersion, originRepo) = await FindPackageAsync(packageId, requestedVersion, repos, cancellationToken);
        _logger.LogInformation("Installing plugin {PackageId} {Version}", packageId, resolvedVersion);

        var marsAssemblyNames = MarsClosure.ReadAssemblyNames(_marsDepsJsonPath);
        var staging = Path.Combine(PluginManager.PluginsDefaultPath, $"_nuget_{Guid.NewGuid():N}");
        var cache = new SourceCacheContext { NoCache = true };

        try
        {
            // корневой пакет: свои сборки без фильтра + фронт-ассеты + дескриптор
            await using var rootStream = await DownloadPackageAsync(originRepo, packageId, resolvedVersion, cache, cancellationToken);
            ExtractPackage(rootStream, staging, marsAssemblyNames, filterLibs: false, cancellationToken);
            await EnqueueAndExtractDependenciesAsync(originRepo, packageId, resolvedVersion, staging, marsAssemblyNames, repos, cache, cancellationToken);

            var descriptorStoragePath = Path.Combine(staging, PluginPackageDescriptor.FileName);
            if (!_fileStorage.FileExists(descriptorStoragePath))
                throw NewValidation($"Package '{packageId}' is not a Mars plugin: '{DescriptorEntry}' not found in the nupkg.");

            var physicalStaging = Path.GetDirectoryName(_fileStorage.FileInfo(descriptorStoragePath).PhysicalPath!)!;
            var descriptor = PluginDescriptorHelper.TryRead(Path.Combine(physicalStaging, PluginPackageDescriptor.FileName))
                ?? throw NewValidation($"Cannot parse '{PluginPackageDescriptor.FileName}' in package '{packageId}'.");

            PluginDescriptorHelper.Validate(descriptor, physicalStaging);

            var finalDir = await PluginInstallFinalizer.FinalizeAsync(_fileStorage, _registry, _logger, staging, descriptor.PackageId, PluginSource.NuGet, resolvedVersion.ToNormalizedString(),
                (from, to) =>
                {
                    _fileStorage.MoveDirectory(from, to);
                    return Task.CompletedTask;
                });
            return new PluginInstallResult(descriptor.PackageId, resolvedVersion.ToNormalizedString(), finalDir);
        }
        catch
        {
            if (_fileStorage.DirectoryExists(staging))
                _fileStorage.DeleteDirectory(staging, recursive: true);
            throw;
        }
    }

    static NuGetVersion? ParseRequestedVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;
        return NuGetVersion.TryParse(version, out var parsed)
            ? parsed
            : throw NewValidation($"Cannot parse version '{version}'.");
    }

    async Task<(NuGetVersion Version, SourceRepository Repo)> FindPackageAsync(string packageId, NuGetVersion? requested,
                                                                                List<SourceRepository> repos, CancellationToken ct)
    {
        foreach (var repo in repos)
        {
            var resource = await repo.GetResourceAsync<FindPackageByIdResource>(ct);
            var versions = (await resource.GetAllVersionsAsync(packageId, new SourceCacheContext(), NuGet.Common.NullLogger.Instance, ct))?.ToList();
            if (versions is null || versions.Count == 0) continue;

            if (requested is not null)
            {
                var match = versions.FirstOrDefault(v => v == requested);
                if (match is null) continue;
                return (match, repo);
            }

            // стабильные версии приоритетнее; пререлизы учитываются, когда стабильных нет
            var best = versions.Where(v => !v.IsPrerelease).DefaultIfEmpty().MaxBy(v => v)
                       ?? versions.MaxBy(v => v)!;
            return (best, repo);
        }

        throw NewValidation($"Package '{packageId}'{(requested is not null ? $" version {requested}" : "")} not found in configured nuget sources.");
    }

    async Task EnqueueAndExtractDependenciesAsync(SourceRepository originRepo, string packageId, NuGetVersion version, string staging,
                                                  HashSet<string> marsAssemblyNames, List<SourceRepository> repos,
                                                  SourceCacheContext cache, CancellationToken ct)
    {
        var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { packageId };
        var queue = new Queue<(string Id, VersionRange Range)>();

        await using (var stream = await DownloadPackageAsync(originRepo, packageId, version, cache, ct))
            foreach (var dep in ReadDependencies(stream))
                queue.Enqueue((dep.Id, dep.VersionRange));

        while (queue.Count > 0)
        {
            var (depId, range) = queue.Dequeue();
            if (!installed.Add(depId)) continue;

            var (depVersion, depRepo) = await ResolveDependencyAsync(depId, range, repos, cache, ct);
            _logger.LogDebug("Dependency {Id} resolved to {Version}", depId, depVersion);

            await using var depStream = await DownloadPackageAsync(depRepo, depId, depVersion, cache, ct);
            ExtractPackage(depStream, staging, marsAssemblyNames, filterLibs: true, ct);

            depStream.Position = 0;
            using var archive = new ZipArchive(depStream, ZipArchiveMode.Read, leaveOpen: true);
            var nuspecEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry is null) continue;

            using var nuspecStream = nuspecEntry.Open();
            var reader = new NuspecReader(nuspecStream);
            foreach (var dep in DependencyGroupsOf(reader))
                foreach (var package in dep.Packages)
                    if (!installed.Contains(package.Id))
                        queue.Enqueue((package.Id, package.VersionRange));
        }
    }

    async Task<(NuGetVersion Version, SourceRepository Repo)> ResolveDependencyAsync(string depId, VersionRange range,
                                                                                     List<SourceRepository> repos, SourceCacheContext cache, CancellationToken ct)
    {
        foreach (var repo in repos)
        {
            var resource = await repo.GetResourceAsync<FindPackageByIdResource>(ct);
            var versions = (await resource.GetAllVersionsAsync(depId, cache, NuGet.Common.NullLogger.Instance, ct))?.ToList();
            if (versions is null || versions.Count == 0) continue;

            // классический резолв нюгета: минимальная версия, удовлетворяющая диапазону
            var satisfying = versions.Where(range.Satisfies).OrderBy(v => v).ToList();
            if (satisfying.Count > 0) return (satisfying[0], repo);
        }

        throw NewValidation($"Dependency '{depId}' ({range}) cannot be resolved in configured nuget sources.");
    }

    static IEnumerable<(string Id, VersionRange VersionRange)> ReadDependencies(Stream nupkgStream)
    {
        using var archive = new ZipArchive(nupkgStream, ZipArchiveMode.Read, leaveOpen: true);
        var nuspecEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
            ?? throw NewValidation("nupkg has no nuspec.");

        using var nuspecStream = nuspecEntry.Open();
        var reader = new NuspecReader(nuspecStream);
        foreach (var group in DependencyGroupsOf(reader))
            foreach (var package in group.Packages)
                yield return (package.Id, package.VersionRange);
    }

    static IEnumerable<PackageDependencyGroup> DependencyGroupsOf(NuspecReader reader)
    {
        var projectTfm = NuGetFramework.Parse($"net{Environment.Version.Major}.{Environment.Version.Minor}");
        var groups = reader.GetDependencyGroups().ToList();

        var compatible = groups.Where(g => g.TargetFramework.IsAny
                                        || DefaultCompatibilityProvider.Instance.IsCompatible(projectTfm, g.TargetFramework)).ToList();

        // из совместимых групп — наиболее специфичная для текущей платформы
        var best = compatible.OrderByDescending(g => g.TargetFramework.IsAny ? Version.Parse("0.0") : g.TargetFramework.Version).FirstOrDefault();
        return best is null ? [] : [best];
    }

    async Task<MemoryStream> DownloadPackageAsync(SourceRepository repo, string id, NuGetVersion version, SourceCacheContext cache, CancellationToken ct)
    {
        var resource = await repo.GetResourceAsync<FindPackageByIdResource>(ct);
        var stream = new MemoryStream();
        var ok = await resource.CopyNupkgToStreamAsync(id, version, stream, cache, NuGet.Common.NullLogger.Instance, ct);
        if (!ok)
            throw NewValidation($"Cannot download package '{id}' {version}.");

        stream.Position = 0;
        return stream;
    }

    void ExtractPackage(Stream nupkgStream, string staging, HashSet<string> marsAssemblyNames, bool filterLibs, CancellationToken ct)
    {
        using var archive = new ZipArchive(nupkgStream, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();

        var libPrefix = PickBestLibPrefix(entries);

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            var name = entry.FullName.Replace('\\', '/');

            string? destination = null;
            if (name.Equals(DescriptorEntry, StringComparison.OrdinalIgnoreCase))
                destination = PluginPackageDescriptor.FileName;
            else if (name.StartsWith(FrontEntryPrefix, StringComparison.OrdinalIgnoreCase))
                destination = Path.Combine("wwwroot", name[FrontEntryPrefix.Length..]);
            else if (libPrefix is not null && name.StartsWith(libPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var fileName = name[libPrefix.Length..];
                var stem = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);

                if (filterLibs)
                {
                    // спутники (.xml/.pdb) сборки, уже имеющейся в Марсе, тоже не нужны;
                    // у спутников сателлитных сборок (X.resources.dll) проверяется базовое имя.
                    var baseStem = stem.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)
                        ? stem[..^".resources".Length]
                        : stem;
                    if ((marsAssemblyNames.Contains(stem) || marsAssemblyNames.Contains(baseStem))
                        && ext is ".dll" or ".xml" or ".pdb")
                    {
                        _logger.LogDebug("Skipping '{File}' — '{Assembly}' is already shipped with Mars.", fileName, baseStem);
                        continue;
                    }

                    // папка плагина — не запускаемое приложение, рантайм-конфиг ему не нужен;
                    // `_._` — маркер «папка пуста», чужие `.deps.json` — артефакты упаковки.
                    if (fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase)
                        || fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
                        || fileName == "_._")
                        continue;
                }

                destination = fileName;
            }

            if (destination is null) continue;

            var destinationPath = Path.Combine(staging, destination);
            var destinationDir = Path.GetDirectoryName(destinationPath)!;
            if (!_fileStorage.DirectoryExists(destinationDir))
                _fileStorage.CreateDirectory(destinationDir);

            using var entryStream = entry.Open();
            _fileStorage.WriteAsync(destinationPath, entryStream, ct).GetAwaiter().GetResult();
        }
    }

    /// <summary>Выбирает лучшую папку `lib/&lt;tfm&gt;/` под текущую платформу.</summary>
    static string? PickBestLibPrefix(List<ZipArchiveEntry> entries)
    {
        var projectTfm = NuGetFramework.Parse($"net{Environment.Version.Major}.{Environment.Version.Minor}");

        var prefixes = entries.Select(e => e.FullName.Replace('\\', '/').Split('/', 3))
                              .Where(parts => parts.Length == 3 && parts[0].Equals("lib", StringComparison.OrdinalIgnoreCase))
                              .Select(parts => parts[1])
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToList();

        NuGetFramework? best = null;
        foreach (var prefix in prefixes)
        {
            var tfm = NuGetFramework.ParseFolder(prefix);
            if (tfm is null || tfm.IsUnsupported) continue;
            if (!tfm.IsAny && !DefaultCompatibilityProvider.Instance.IsCompatible(projectTfm, tfm)) continue;

            if (best is null || (!tfm.IsAny && tfm.Version > (best.IsAny ? Version.Parse("0.0") : best.Version)))
                best = tfm;
        }

        return best is null ? null : $"lib/{best.GetShortFolderName()}/";
    }

    static MarsValidationException NewValidation(string message)
        => new(message, new Dictionary<string, string[]>());
}
