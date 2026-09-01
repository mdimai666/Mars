using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Core.Exceptions;
using Mars.Options.Abstractions.Services;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Abstractions.Services;
using Mars.Plugin.Contracts.Options;
using Mars.Plugin.Dto;
using Mars.Plugin.Handlers;
using Mars.Plugin.Mappings;
using Mars.Server.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Plugin.Services;

internal class PluginService : IPluginService
{
    private readonly IFileStorage _fileStorage;
    private readonly PluginManager _pluginManager;
    private readonly IOptionService _optionService;

    public static readonly string ErrorNotAllowUploadZipManuallyMessage = "Upload plugin disallowed in settings";
    public static readonly string ErrorPluginBlockedMessage = "Plugin installation is blocked by settings";
    public static readonly string ErrorPluginLockedMessage = "Plugin is locked by configuration — cannot be modified.";
    internal IReadOnlyCollection<LoadedPlugin> Plugins => _pluginManager.Plugins;

    public PluginService([FromKeyedServices("data")] IFileStorage fileStorage, PluginManager pluginManager, IOptionService optionService)
    {
        _fileStorage = fileStorage;
        _pluginManager = pluginManager;
        _optionService = optionService;
    }

    public ListDataResult<PluginInfoDto> List(ListPluginQuery query)
    {
        return Plugins.Where(s => (query.Search == null || s.Info.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
                        .Select(s => s.Info.ToInfoDto())
                        .AsListDataResult(query);
    }

    public PagingResult<PluginInfoDto> ListTable(ListPluginQuery query)
    {
        return Plugins.Where(s => (query.Search == null || s.Info.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
                        .Select(s => s.Info.ToInfoDto())
                        .AsPagingResult(query);
    }

    public IDictionary<string, PluginManifestInfoDto> RuntimePluginManifests()
    {
        return Plugins.Where(s => s.Info.ManifestFile != null)
                        .Select(s => new PluginManifestInfoDto { Name = s.Info.KeyName, Uri = s.Info.ManifestFile! })
                        .ToDictionary(s => s.Name);
    }

    public Task<PluginsUploadOperationResultDto> UploadPlugin(IFormFileCollection files, CancellationToken cancellationToken)
    {
        var pluginOptions = _optionService.GetOption<PluginManagerSettingsOption>();
        if (!pluginOptions.AllowUploadZipManually)
            throw new UserActionException(ErrorNotAllowUploadZipManuallyMessage);

        var handler = new PluginZipInstaller(_fileStorage, MarsLogger.GetStaticLogger<PluginZipInstaller>(), _pluginManager.Registry);
        return handler.Handle(files, cancellationToken);
    }

    public async Task<PluginInstallResultDto> InstallFromNuget(string packageId, string? version, CancellationToken cancellationToken)
    {
        var pluginOptions = _optionService.GetOption<PluginManagerSettingsOption>();
        if (pluginOptions.GetBlockedPackageIds().Contains(packageId))
            throw new UserActionException(ErrorPluginBlockedMessage);

        var sources = pluginOptions.GetNugetSources().ToList();
        var installer = new PluginNugetInstaller(_fileStorage, MarsLogger.GetStaticLogger<PluginNugetInstaller>(), _pluginManager.Registry);
        var result = await installer.InstallAsync(packageId, version, sources, cancellationToken);

        return new PluginInstallResultDto
        {
            PackageId = result.PackageId,
            Version = result.Version,
            InstalledAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public Task SetEnabled(string packageId, bool enabled)
    {
        var info = FindOrThrow(packageId);
        if (info.Locked)
            throw new UserActionException(ErrorPluginLockedMessage);

        _pluginManager.Registry.SetDisabled(packageId, !enabled);
        return Task.CompletedTask;
    }

    public Task Uninstall(string packageId)
    {
        var info = FindOrThrow(packageId);
        if (info.Locked)
            throw new UserActionException(ErrorPluginLockedMessage);

        var dir = Path.Combine(PluginManager.PluginsDefaultPath, packageId);
        if (_fileStorage.DirectoryExists(dir))
            _fileStorage.DeleteDirectory(dir, recursive: true);

        _pluginManager.Registry.Remove(packageId);
        return Task.CompletedTask;
    }

    PluginInfo FindOrThrow(string packageId)
    {
        var info = Plugins.Select(p => p.Info).FirstOrDefault(i => i.PackageId == packageId);
        return info ?? throw new UserActionException($"Plugin '{packageId}' is not installed.");
    }

}
