using System.IO.Compression;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Plugins;
using Mars.Host.Shared.Services;
using Mars.Plugin.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Plugin.Handlers;

internal class PluginZipInstaller
{
    internal readonly string[] AllowedContentTypes = ["application/zip", "application/x-zip-compressed"];
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<PluginZipInstaller> _logger;

    public PluginZipInstaller([FromKeyedServices("data")] IFileStorage fileStorage, ILogger<PluginZipInstaller> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<PluginsUploadOperationResultDto> Handle(IFormFileCollection files, CancellationToken cancellationToken)
    {
        CheckRequiredDiskSizeAndPermissions();
        var pluginsDir = PluginService.PluginsDefaultPath;

        // Проверка, что все файлы — ZIP
        foreach (var file in files)
        {
            if (!AllowedContentTypes.Contains(file.ContentType))
                throw new MarsValidationException($"Only ZIP files are allowed. '{file.FileName}' is of type '{file.ContentType}' (Content-Type=[application/zip, application/x-zip-compressed]).", new Dictionary<string, string[]>());
        }

        var uploadStart = DateTimeOffset.Now;
        var list = new List<PluginsUploadOperationItemDto>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pluginIndividualDir = Path.Combine(pluginsDir, Path.GetFileNameWithoutExtension(file.FileName));

            _logger.LogInformation("UnpackFiles: start upload plugin to '{PluginPath}' from file '{FileName}'", pluginIndividualDir, file.FileName);
            await UnpackFiles(pluginIndividualDir, file, _fileStorage, cancellationToken);
            _logger.LogInformation("InstallPlugin: upsert '{PluginPath}'", pluginIndividualDir);
            await InstallPlugin(pluginIndividualDir, _fileStorage, cancellationToken);

            list.Add(new PluginsUploadOperationItemDto
            {
                FileName = file.FileName,
                FileSize = file.Length,
                ErrorMessage = null,
            });
            _logger.LogInformation("Complete '{PluginPath}'", pluginIndividualDir);
        }

        var uploadEnd = DateTimeOffset.Now;

        return new PluginsUploadOperationResultDto
        {
            UploadStart = uploadStart,
            UploadEnd = uploadEnd,
            Items = list
        };
    }

    void CheckRequiredDiskSizeAndPermissions()
    {
        // TODO
    }

    private async Task UnpackFiles(string pluginsIndividualDir, IFormFile file, IFileStorage fileStorage, CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name))
                continue; // Пропускаем папки

            var destinationPath = Path.Combine(pluginsIndividualDir, entry.FullName);
            var destinationDir = Path.GetDirectoryName(destinationPath)!;

            if (!fileStorage.DirectoryExists(destinationDir))
                fileStorage.CreateDirectory(destinationDir);

            using var entryStream = entry.Open();
            //using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            //await entryStream.CopyToAsync(fileStream, cancellationToken);
            await fileStorage.WriteAsync(destinationPath, entryStream, cancellationToken);
        }
    }

    private Task InstallPlugin(string pluginsIndividualDir, IFileStorage fileStorage, CancellationToken cancellationToken)
    {
        //throw new NotImplementedException("Plugin installation logic is not implemented yet. Please implement the installation logic for the plugin located at: " + pluginsIndividualDir);
        return Task.CompletedTask;
    }
}
