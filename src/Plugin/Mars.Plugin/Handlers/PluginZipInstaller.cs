using System.IO.Compression;
using Mars.Core.Exceptions;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Contracts.Plugins;
using Mars.Plugin.Services;
using Mars.Server.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Plugin.Handlers;

internal class PluginZipInstaller
{
    internal readonly string[] AllowedContentTypes = ["application/zip", "application/x-zip-compressed"];
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<PluginZipInstaller> _logger;
    private readonly PluginRegistry _registry;

    public PluginZipInstaller([FromKeyedServices("data")] IFileStorage fileStorage, ILogger<PluginZipInstaller> logger, PluginRegistry registry)
    {
        _fileStorage = fileStorage;
        _logger = logger;
        _registry = registry;
    }

    public async Task<PluginsUploadOperationResultDto> Handle(IFormFileCollection files, CancellationToken cancellationToken)
    {
        CheckRequiredDiskSizeAndPermissions();
        var pluginsDir = PluginManager.PluginsDefaultPath;

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

            var fallbackName = Path.GetFileNameWithoutExtension(file.FileName);
            var stagingDir = Path.Combine(pluginsDir, $"_upload_{Guid.NewGuid():N}");

            _logger.LogInformation("UnpackFiles: start upload plugin to '{StagingPath}' from file '{FileName}'", stagingDir, file.FileName);
            await UnpackFiles(stagingDir, file, _fileStorage, cancellationToken);

            string installedDir;
            try
            {
                installedDir = await InstallPlugin(stagingDir, fallbackName, cancellationToken);
            }
            catch
            {
                try
                {
                    if (_fileStorage.DirectoryExists(stagingDir))
                        _fileStorage.DeleteDirectory(stagingDir, recursive: true);
                }
                catch (Exception cleanupEx)
                {
                    // staging мог остаться залоченным тем же процессом, что сорвал установку;
                    // мусор безопасен — папки с префиксом «_» при загрузке плагинов пропускаются
                    _logger.LogWarning(cleanupEx, "Failed to clean up staging folder '{StagingPath}'", stagingDir);
                }
                throw;
            }

            list.Add(new PluginsUploadOperationItemDto
            {
                FileName = file.FileName,
                FileSize = file.Length,
                ErrorMessage = null,
            });
            _logger.LogInformation("Complete '{PluginPath}'", installedDir);
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
            await fileStorage.WriteAsync(destinationPath, entryStream, cancellationToken);
        }
    }

    /// <summary>
    /// Валидирует распакованный плагин и кладёт его в окончательную папку
    /// `plugins/&lt;PackageId&gt;` (старая версия того же пакета заменяется).
    /// </summary>
    /// <returns>путь установленной папки (относительно data)</returns>
    private async Task<string> InstallPlugin(string stagingDir, string fallbackName, CancellationToken cancellationToken)
    {
        UnwrapSingleBaseFolder(stagingDir);

        var descriptorPath = Path.Combine(stagingDir, PluginPackageDescriptor.FileName);
        string targetName;
        string version = "0";

        if (_fileStorage.FileExists(descriptorPath))
        {
            var physicalStaging = _fileStorage.FileInfo(descriptorPath).PhysicalPath!;
            var descriptor = PluginDescriptorHelper.TryRead(physicalStaging)
                ?? throw new MarsValidationException($"Cannot parse {PluginPackageDescriptor.FileName} in plugin zip.", new Dictionary<string, string[]>());

            PluginDescriptorHelper.Validate(descriptor, Path.GetDirectoryName(physicalStaging)!);
            targetName = string.IsNullOrWhiteSpace(descriptor.PackageId) ? fallbackName : descriptor.PackageId;
            version = descriptor.Version;
        }
        else
        {
            // легаси-раскладка без дескриптора — по .runtimeconfig рядом с входной сборкой
            var hasRuntimeConfig = _fileStorage.GetDirectoryContents(stagingDir).Any(s => s.Name.EndsWith(".runtimeconfig.json"));
            if (!hasRuntimeConfig)
                throw new MarsValidationException($"Plugin zip contains neither '{PluginPackageDescriptor.FileName}' nor a .runtimeconfig.json entry assembly.", new Dictionary<string, string[]>());

            _logger.LogWarning("Plugin zip has no {Descriptor} — installed as legacy layout.", PluginPackageDescriptor.FileName);
            targetName = fallbackName;
        }

        return await PluginInstallFinalizer.FinalizeAsync(_fileStorage, _registry, _logger, stagingDir, targetName, PluginSource.Zip, version,
            (from, to) => MoveInstalledPluginAsync(from, to, cancellationToken));
    }

    /// <summary>
    /// Перенос с ретраями: свежезапакованные сборки некоторое время удерживает
    /// антивирус/индексатор, и разовый <c>Directory.Move</c> падает «Access denied»
    /// (залочен файл внутри переносимой папки); лок спадает сам за секунды.
    /// </summary>
    internal async Task MoveInstalledPluginAsync(string stagingDir, string finalDir, CancellationToken cancellationToken,
                                                 int attempts = 5, TimeSpan? initialDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromMilliseconds(300);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                _fileStorage.MoveDirectory(stagingDir, finalDir);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt >= attempts)
                    throw new UserActionException(
                        "Failed to install the plugin: its files are locked by another process (usually an antivirus scanning freshly unpacked assemblies). Try uploading the zip again in a few seconds.",
                        null, ex);

                _logger.LogWarning("Plugin folder move is busy ({Attempt}/{Attempts}) — files are likely locked by an antivirus scan, retrying", attempt, attempts);
                await Task.Delay(delay, cancellationToken);
                delay += delay;
            }
        }
    }

    /// <summary>Рукотворные архивы часто несут одну базовую папку — выворачиваем её в корень.</summary>
    private void UnwrapSingleBaseFolder(string stagingDir)
    {
        bool HasMarker(string dir) => _fileStorage.GetDirectoryContents(dir)
            .Any(s => s.Name == PluginPackageDescriptor.FileName || s.Name.EndsWith(".runtimeconfig.json"));

        var entries = _fileStorage.GetDirectoryContents(stagingDir).ToList();
        if (entries.Any(s => s.Name == PluginPackageDescriptor.FileName || s.Name.EndsWith(".runtimeconfig.json")))
            return;

        var singleDir = entries.Count == 1 && entries[0].IsDirectory ? entries[0] : null;
        if (singleDir is null || !HasMarker(Path.Combine(stagingDir, singleDir.Name)))
            return;

        var innerDir = Path.Combine(stagingDir, singleDir.Name);
        var tmpDir = $"{stagingDir}_inner";
        _fileStorage.MoveDirectory(innerDir, tmpDir);
        _fileStorage.DeleteDirectory(stagingDir, recursive: true);
        _fileStorage.MoveDirectory(tmpDir, stagingDir);
    }
}
