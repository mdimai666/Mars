//#define USE_EXAMPLE_PLUGINS
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Plugins;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Mars.Plugin.Dto;
using Mars.Plugin.Handlers;
using Mars.Plugin.Mappings;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Plugin.Services;

internal class PluginService : IPluginService
{
    private readonly IFileStorage _fileStorage;
    private readonly PluginManager _pluginManager;
    private readonly IOptionService _optionService;
    public const string PluginsDefaultPath = "plugins";
    public static readonly string ErrorNotAllowUploadZipManuallyMessage = "Upload plugin disallowed in settings";
    internal IReadOnlyCollection<PluginData> Plugins => _pluginManager.Plugins;

    public PluginService([FromKeyedServices("data")] IFileStorage fileStorage, PluginManager pluginManager, IOptionService optionService)
    {
        _fileStorage = fileStorage;
        _pluginManager = pluginManager;
        _optionService = optionService;
        if (!_fileStorage.DirectoryExists(PluginsDefaultPath)) _fileStorage.CreateDirectory(PluginsDefaultPath);
    }

    public ListDataResult<PluginInfoDto> List(ListPluginQuery query)
    {
#if USE_EXAMPLE_PLUGINS
        return PluginExampleData.GetExamplePluginList(query).AsListDataResult(query);
#endif

        return Plugins.Where(s => (query.Search == null || s.Info.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
                        .Select(s => s.Info.ToInfoDto())
                        .AsListDataResult(query);
    }

    public PagingResult<PluginInfoDto> ListTable(ListPluginQuery query)
    {
#if USE_EXAMPLE_PLUGINS
        return PluginExampleData.GetExamplePluginList(query).AsPagingResult(query);
#endif
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

        var handler = new PluginZipInstaller(_fileStorage, MarsLogger.GetStaticLogger<PluginZipInstaller>());
        return handler.Handle(files, cancellationToken);
    }

}
