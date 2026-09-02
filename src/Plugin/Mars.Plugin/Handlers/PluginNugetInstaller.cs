using System.Text.Json;
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
using NuGet.Packaging.Core;
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
    private readonly string _resolveCachePath;

    public PluginNugetInstaller(IFileStorage fileStorage, ILogger logger, PluginRegistry? registry = null, string? marsDepsJsonPath = null)
    {
        _fileStorage = fileStorage;
        _logger = logger;
        _registry = registry ?? new PluginRegistry(fileStorage);
        _marsDepsJsonPath = marsDepsJsonPath ?? Path.Combine(AppContext.BaseDirectory, "Mars.deps.json");
        _resolveCachePath = Path.Combine(PluginManager.PluginsDefaultPath, ResolveCache.FileName);
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
        var marsPackageIds = MarsClosure.ReadClosurePackageIds(_marsDepsJsonPath);
        var staging = Path.Combine(PluginManager.PluginsDefaultPath, $"_nuget_{Guid.NewGuid():N}");
        var cache = new SourceCacheContext();
        var resolveCache = ResolveCache.Load(_fileStorage, _resolveCachePath);

        try
        {
            // корневой пакет: свои сборки без фильтра + фронт-ассеты + дескриптор + иконка
            string? iconFile;
            List<PackageDependency> rootDependencies;
            using (var rootResult = await DownloadPackageAsync(originRepo, packageId, resolvedVersion, cache, cancellationToken))
            {
                iconFile = ExtractPackage(rootResult.PackageReader, staging, marsAssemblyNames, filterLibs: false, includeIcon: true, cancellationToken);
                rootDependencies = DependencyPackagesOf(rootResult.PackageReader).ToList();
            }

            await EnqueueAndExtractDependenciesAsync(rootDependencies, staging, marsAssemblyNames, marsPackageIds, repos, cache, resolveCache, cancellationToken);

            var descriptorStoragePath = Path.Combine(staging, PluginPackageDescriptor.FileName);
            if (!_fileStorage.FileExists(descriptorStoragePath))
                throw NewValidation($"Package '{packageId}' is not a Mars plugin: '{DescriptorEntry}' not found in the nupkg.");

            var physicalStaging = Path.GetDirectoryName(_fileStorage.FileInfo(descriptorStoragePath).PhysicalPath!)!;
            var descriptor = PluginDescriptorHelper.TryRead(Path.Combine(physicalStaging, PluginPackageDescriptor.FileName))
                ?? throw NewValidation($"Cannot parse '{PluginPackageDescriptor.FileName}' in package '{packageId}'.");

            PluginDescriptorHelper.Validate(descriptor, physicalStaging);

            await EnrichDescriptorWithNugetMetadataAsync(originRepo, packageId, resolvedVersion, descriptor, iconFile, physicalStaging, cancellationToken);

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
        finally
        {
            resolveCache.Save(_fileStorage, _resolveCachePath);
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

    async Task EnqueueAndExtractDependenciesAsync(List<PackageDependency> rootDependencies, string staging,
                                                  HashSet<string> marsAssemblyNames, HashSet<string> marsPackageIds,
                                                  List<SourceRepository> repos, SourceCacheContext cache,
                                                  ResolveCache resolveCache, CancellationToken ct)
    {
        var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { };
        var queue = new Queue<(string Id, VersionRange Range)>();

        foreach (var dep in rootDependencies)
            EnqueueIfRelevant(dep.Id, dep.VersionRange, marsAssemblyNames, marsPackageIds, queue);

        while (queue.Count > 0)
        {
            var (depId, range) = queue.Dequeue();
            if (!installed.Add(depId)) continue;

            var (depVersion, depRepo) = await ResolveDependencyAsync(depId, range, repos, cache, resolveCache, ct);
            _logger.LogDebug("Dependency {Id} resolved to {Version}", depId, depVersion);

            using var depResult = await DownloadPackageAsync(depRepo, depId, depVersion, cache, ct);
            ExtractPackage(depResult.PackageReader, staging, marsAssemblyNames, filterLibs: true, includeIcon: false, ct);

            foreach (var dep in DependencyPackagesOf(depResult.PackageReader))
                if (!installed.Contains(dep.Id))
                    EnqueueIfRelevant(dep.Id, dep.VersionRange, marsAssemblyNames, marsPackageIds, queue);
        }
    }

    /// <summary>
    /// Пакет из замыкания Марса (или дающий только имеющиеся в Марсе сборки) не
    /// резолвится и не скачивается: его рантайм-сборки отдаёт хост.
    /// </summary>
    void EnqueueIfRelevant(string depId, VersionRange range, HashSet<string> marsAssemblyNames, HashSet<string> marsPackageIds,
                           Queue<(string Id, VersionRange Range)> queue)
    {
        if (marsPackageIds.Contains(depId) || marsAssemblyNames.Contains(depId))
        {
            _logger.LogDebug("Skipping package '{Id}' — already in the Mars closure.", depId);
            return;
        }

        queue.Enqueue((depId, range));
    }

    async Task<(NuGetVersion Version, SourceRepository Repo)> ResolveDependencyAsync(string depId, VersionRange range,
                                                                                     List<SourceRepository> repos, SourceCacheContext cache,
                                                                                     ResolveCache resolveCache, CancellationToken ct)
    {
        var cacheKey = $"{depId}|{range.OriginalString}";
        if (resolveCache.TryGet(cacheKey, out var cached) && cached.SourceIndex >= 0 && cached.SourceIndex < repos.Count)
        {
            _logger.LogDebug("Dependency {Id} resolved to {Version} (resolve cache)", depId, cached.Version);
            return (NuGetVersion.Parse(cached.Version), repos[cached.SourceIndex]);
        }

        for (var i = 0; i < repos.Count; i++)
        {
            var resource = await repos[i].GetResourceAsync<FindPackageByIdResource>(ct);
            var versions = (await resource.GetAllVersionsAsync(depId, cache, NuGet.Common.NullLogger.Instance, ct))?.ToList();
            if (versions is null || versions.Count == 0) continue;

            // классический резолв нюгета: минимальная версия, удовлетворяющая диапазону
            var satisfying = versions.Where(range.Satisfies).OrderBy(v => v).ToList();
            if (satisfying.Count == 0) continue;

            resolveCache.Set(cacheKey, satisfying[0].ToNormalizedString(), i);
            return (satisfying[0], repos[i]);
        }

        throw NewValidation($"Dependency '{depId}' ({range}) cannot be resolved in configured nuget sources.");
    }

    /// <summary>
    /// Контент пакета через глобальную папку nuget (`~/.nuget/packages`): если
    /// пакет@версия уже на диске — читается локально без сети, иначе скачивается
    /// в неё (стандартное поведение `dotnet restore`).
    /// </summary>
    static async Task<DownloadResourceResult> DownloadPackageAsync(SourceRepository repo, string id, NuGetVersion version,
                                                                   SourceCacheContext cache, CancellationToken ct)
    {
        var downloadResource = await repo.GetResourceAsync<DownloadResource>(ct);
        var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null));
        var result = await downloadResource.GetDownloadResourceResultAsync(
            new PackageIdentity(id, version), new PackageDownloadContext(cache),
            globalPackagesFolder, NuGet.Common.NullLogger.Instance, ct);

        if (result.PackageReader is null)
            throw NewValidation($"Cannot download package '{id}' {version}.");

        return result;
    }

    static IEnumerable<PackageDependency> DependencyPackagesOf(PackageReaderBase reader)
    {
        var projectTfm = NuGetFramework.Parse($"net{Environment.Version.Major}.{Environment.Version.Minor}");
        var groups = reader.NuspecReader.GetDependencyGroups().ToList();

        var compatible = groups.Where(g => g.TargetFramework.IsAny
                                        || DefaultCompatibilityProvider.Instance.IsCompatible(projectTfm, g.TargetFramework)).ToList();

        // из совместимых групп — наиболее специфичная для текущей платформы
        var best = compatible.OrderByDescending(g => g.TargetFramework.IsAny ? Version.Parse("0.0") : g.TargetFramework.Version).FirstOrDefault();
        return best?.Packages ?? [];
    }

    /// <summary>
    /// Раскладывает пакет в `staging`. Возвращает имя файла иконки, если она была
    /// извлечена в `wwwroot/` (для корневых пакетов с `includeIcon`).
    /// </summary>
    string? ExtractPackage(PackageReaderBase reader, string staging, HashSet<string> marsAssemblyNames, bool filterLibs, bool includeIcon, CancellationToken ct)
    {
        var entries = reader.GetFiles().Where(n => !string.IsNullOrEmpty(Path.GetFileName(n))).ToList();
        var libPrefix = PickBestLibPrefix(entries);
        var iconEntry = includeIcon ? reader.NuspecReader.GetIcon() : null;
        string? iconFile = null;

        foreach (var rawName in entries)
        {
            ct.ThrowIfCancellationRequested();
            var name = rawName.Replace('\\', '/');

            string? destination = null;
            if (name.Equals(DescriptorEntry, StringComparison.OrdinalIgnoreCase))
                destination = PluginPackageDescriptor.FileName;
            else if (name.StartsWith(FrontEntryPrefix, StringComparison.OrdinalIgnoreCase))
                destination = Path.Combine("wwwroot", name[FrontEntryPrefix.Length..]);
            else if (iconEntry is not null && name.Equals(iconEntry.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                destination = Path.Combine("wwwroot", iconFile = Path.GetFileName(iconEntry));
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

            using var entryStream = reader.GetStream(rawName);
            _fileStorage.WriteAsync(destinationPath, entryStream, ct).GetAwaiter().GetResult();
        }

        return iconFile;
    }

    async Task EnrichDescriptorWithNugetMetadataAsync(SourceRepository originRepo, string packageId, NuGetVersion version,
                                                        PluginPackageDescriptor descriptor, string? iconFile,
                                                        string physicalStaging, CancellationToken ct)
    {
        try
        {
            var resource = await originRepo.GetResourceAsync<PackageMetadataResource>(ct);
            var metadata = await resource.GetMetadataAsync(new PackageIdentity(packageId, version), new SourceCacheContext(), NuGet.Common.NullLogger.Instance, ct);

            if (!string.IsNullOrWhiteSpace(metadata?.Title)) descriptor.Title = metadata!.Title;
            if (!string.IsNullOrWhiteSpace(metadata?.Description)) descriptor.Description = metadata!.Description;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cannot read nuget metadata for '{PackageId}' — list falls back to assembly attributes.", packageId);
        }

        descriptor.IconFile = iconFile;

        var descriptorPath = Path.Combine(physicalStaging, PluginPackageDescriptor.FileName);
        File.WriteAllText(descriptorPath, JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Выбирает лучшую папку `lib/&lt;tfm&gt;/` под текущую платформу.</summary>
    static string? PickBestLibPrefix(List<string> entryNames)
    {
        var projectTfm = NuGetFramework.Parse($"net{Environment.Version.Major}.{Environment.Version.Minor}");

        var prefixes = entryNames.Select(n => n.Replace('\\', '/').Split('/', 3))
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
