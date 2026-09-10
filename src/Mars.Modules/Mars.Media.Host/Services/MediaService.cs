using Mars.Contracts.Common;
using Mars.Contracts.Dto.Files;
using Mars.Core.Exceptions;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Repositories;
using Mars.Media.Abstractions.Services;
using Mars.Media.Contracts.Options;
using Mars.Options.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Mars.Server.Abstractions.Startup;
using Mars.Server.Abstractions.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Mars.Media.Host.Services;

internal class MediaService : FileService, IMediaService, IMarsAppLifetimeService
{
    private readonly ILogger<MediaService> _logger;
    private readonly IMediaFolderService _folderService;
    internal string MediaDirByYear => MediaDirName + '/' + DateTimeOffset.Now.Year;

    public MediaService(
        IFileStorage fileStorage,
        IOptionService optionService,
        IFileRepository fileRepository,
        IImageProcessor imageProcessor,
        IValidatorFactory validatorFactory,
        IMediaFolderService folderService,
        ILogger<MediaService> logger)
        : base(
            fileStorage,
            optionService,
            fileRepository,
            imageProcessor,
            validatorFactory)
    {
        _folderService = folderService;
        _logger = logger;
    }

    public new Task OnStartupAsync()
    {
        EnsureMediaYearDirExist();
        return Task.CompletedTask;
    }

    void EnsureMediaYearDirExist()
    {
        if (!_fileStorage.DirectoryExists(MediaDirByYear))
        {
            _fileStorage.CreateDirectory(MediaDirByYear);
        }
    }

    public async Task<Guid> WriteUploadToMedia(IFormFile formFile, Guid userId, CancellationToken cancellationToken, Guid? folderId = null, string? folderPath = null)
    {
        MediaFolderDto folder;
        if (folderId is not null)
        {
            folder = await _folderService.GetById(folderId.Value, cancellationToken)
                ?? throw new NotFoundException("Папка не найдена");
        }
        else if (!string.IsNullOrWhiteSpace(folderPath))
        {
            // папка по пути (например, из настроек мета-поля); создаётся при отсутствии
            folder = await _folderService.GetOrCreateByPath(folderPath.Trim(), userId, cancellationToken);
        }
        else
        {
            // загрузка в корень: как и раньше в Media/{год}, с привязкой к папке года
            folder = await _folderService.GetOrCreateByPath(MediaDirByYear, userId, cancellationToken);
        }

        return await WriteUpload(formFile, folder.Path, userId, cancellationToken, folder.Id);
    }

    public async Task<UserActionResult> ScanFilesAndSaveInDB(Guid userId, CancellationToken cancellationToken)
    {
        var files = ScanFiles(MediaDirName);
        var existInDbFiles = await _fileRepository.ListAllRelativePaths(cancellationToken);
        var nonExistFiles = files.Except(existInDbFiles).ToList();

        // зарегистрировать найденные каталоги как папки в БД
        var dirPaths = files
            .Where(p => p.Contains('/'))
            .Select(p => p[..p.LastIndexOf('/')])
            .Distinct();
        var pathToFolderId = await _folderService.EnsureFoldersByPaths(dirPaths, userId, cancellationToken);

        var fileEntities = new List<CreateFileQuery>(nonExistFiles.Count);

        foreach (var filePathFromUpload in nonExistFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileDir = filePathFromUpload.Contains('/') ? filePathFromUpload[..filePathFromUpload.LastIndexOf('/')] : null;

            var createQuery = new CreateFileQuery
            {
                FilePathFromUpload = filePathFromUpload,
                Name = Path.GetFileName(filePathFromUpload),
                Size = (ulong)(_fileStorage.GetFileInfo(filePathFromUpload)?.Length ?? 0),
                UserId = userId,
                Meta = null, // call regenerate thumbs after this
                FolderId = fileDir is not null ? pathToFolderId.GetValueOrDefault(fileDir) : null,
            };

            fileEntities.Add(createQuery);
        }

        await _fileRepository.CreateMany(fileEntities, _hostingInfo, cancellationToken);

        // привязать к папкам файлы, которые были в базе без папки
        var unassignedFiles = await _fileRepository.ListAll(new ListAllFileQuery { FolderId = Guid.Empty }, _hostingInfo, cancellationToken);
        var assignUpdates = new List<FileMoveUpdate>();

        foreach (var file in unassignedFiles)
        {
            if (!file.FilePhysicalPath.Contains('/')) continue;

            var fileDir = file.FilePhysicalPath[..file.FilePhysicalPath.LastIndexOf('/')];
            if (!pathToFolderId.TryGetValue(fileDir, out var assignedFolderId)) continue;

            assignUpdates.Add(new FileMoveUpdate
            {
                Id = file.Id,
                FilePhysicalPath = file.FilePhysicalPath,
                FileVirtualPath = file.FileVirtualPath,
                FolderId = assignedFolderId,
            });
        }

        if (assignUpdates.Count > 0)
        {
            await _fileRepository.UpdateAfterMove(assignUpdates, _hostingInfo, cancellationToken);
        }

        return new UserActionResult
        {
            Ok = true,
            Message = $"Файлов добавлено: {fileEntities.Count}"
        };
    }

    public async Task<UserActionResult> GenerateThumbnails(bool onlyWithEmptyMeta, CancellationToken cancellationToken)
    {
        FileDetail? currentFileForException = null;
        try
        {
            var mediaOption = _optionService.GetOption<MediaOption>();
            var thumbOptions = mediaOption.ImagePreviewSizeConfigs;

            var query = new ListAllFileQuery { IsImage = true };

            var files = await _fileRepository.ListAllDetail(query, _hostingInfo, cancellationToken);

            var updateQueryList = new List<UpdateFileQuery>(files.Count);

            foreach (var _file in files)
            {
                if (onlyWithEmptyMeta && _file.Meta != null) continue;

                cancellationToken.ThrowIfCancellationRequested();

                var file = _file;

                currentFileForException = file;
                ImageInfoDto? imageMeta = null;
                var thumbnails = new Dictionary<string, ImageThumbnailDto>(thumbOptions.Length);

                //string fullFilePath = _storage.FullFilePath(file.FilePath);

                if (!file.IsSvg && (file.Meta.ImageInfo?.Width == 0))
                {

                    try
                    {
                        using var fileStream = _fileStorage.OpenRead(file.FilePhysicalPath);
                        var image = _imageProcessor.ImageSize(fileStream);
                        imageMeta = new ImageInfoDto { Width = image.Width, Height = image.Height };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "error on read image size from stream");
                    }
                }

                if (file.IsSvg)
                {
                    foreach (var cfg in thumbOptions)
                    {
                        thumbnails.Add(cfg.Name, GetImageThumbnail(cfg, file.FilePhysicalPath));
                    }
                }
                else
                {
                    using var sourceStream = _fileStorage.OpenRead(file.FilePhysicalPath);

                    foreach (var cfg in thumbOptions)
                    {
                        string thumbFilepath = GenerateImageThumbPath(cfg, file.FilePhysicalPath);
                        var thumFileDir = _hostingInfo.NormalizePathSlashes(Path.GetDirectoryName(thumbFilepath))!;
                        if (!_fileStorage.DirectoryExists(thumFileDir)) _fileStorage.CreateDirectory(thumFileDir);

                        using var thumbStream = new MemoryStream();
                        sourceStream.Position = 0;
                        _imageProcessor.ProcessImage(sourceStream, thumbStream, cfg);
                        thumbStream.Position = 0;
                        _fileStorage.Write(thumbFilepath, thumbStream);

                        thumbnails.Add(cfg.Name, GetImageThumbnail(cfg, thumbFilepath));
                    }
                }

                updateQueryList.Add(new UpdateFileQuery
                {
                    Id = file.Id,
                    Name = file.Name,
                    Meta = new FileEntityMetaDto
                    {
                        ImageInfo = imageMeta,
                        Thumbnails = thumbnails
                    }
                });
            }

            //await ef.SaveChangesAsync();
            await _fileRepository.UpdateBulk(updateQueryList, _hostingInfo, cancellationToken);

            return new UserActionResult
            {
                Ok = true,
                Message = $"Успешно созданы миниатюры для {files.Count} изображений"
            };
        }
        catch (Exception ex)
        {
            return new UserActionResult
            {
                Message = (currentFileForException is null) ? ex.Message : $"filepath: {currentFileForException.FilePhysicalPath}\n{ex.Message}",
            };
        }
    }

    #region TOOLS
    List<string> ScanFiles(string rootPath)
    {
        string[] ignoreList = ["bin", "obj", ".git", "node_modules"];

        var pathsToSearch = new Queue<string>();
        var foundFiles = new List<string>();

        pathsToSearch.Enqueue(rootPath);

        while (pathsToSearch.Count > 0)
        {
            var dir = pathsToSearch.Dequeue();

            if (!_fileStorage.DirectoryExists(dir)) continue;

            foreach (var entry in _fileStorage.GetDirectoryContents(dir))
            {
                var entryPath = dir.Length == 0 ? entry.Name : dir + '/' + entry.Name;

                if (!entry.IsDirectory)
                {
                    foundFiles.Add(entryPath);
                    continue;
                }

                if (ignoreList.Contains(entry.Name)) continue;
                pathsToSearch.Enqueue(entryPath);
            }
        }

        return foundFiles;
    }
    #endregion
}
