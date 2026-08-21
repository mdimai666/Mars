using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Host.Shared.Validators;
using Mars.Options.Models;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Services;

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

    public async Task<Guid> WriteUploadToMedia(IFormFile formFile, Guid userId, CancellationToken cancellationToken, Guid? folderId = null)
    {
        MediaFolderDto folder;
        if (folderId is not null)
        {
            folder = await _folderService.GetById(folderId.Value, cancellationToken)
                ?? throw new NotFoundException("Папка не найдена");
        }
        else
        {
            // загрузка в корень: как и раньше в Media/{год}, с привязкой к папке года
            folder = await _folderService.GetOrCreateByPath(MediaDirByYear, userId, cancellationToken);
        }

        return await WriteUpload(formFile, folder.Path, userId, cancellationToken, folder.Id);
    }

    public async Task<UserActionResult> ExecuteAction(ExecuteActionRequest action, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            if (action.ActionId == "test")
            {
                return new UserActionResult
                {
                    Message = "test successfully",
                    Ok = true
                };
            }
            else if (action.ActionId == "ScanFiles")
            {
                return await ScanFilesAndSaveInDB(userId, cancellationToken);
            }
            else if (action.ActionId == "GenerateThumbnails")
            {
                return await GenerateThumbnails(false, cancellationToken);
            }
            else
            {
                return new UserActionResult
                {
                    Message = $"Action \"{action.ActionId}\" not found",
                    Ok = true
                };
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            //throw;
            return new UserActionResult
            {
                Message = ex.Message
            };
        }
    }

    public async Task<UserActionResult> ScanFilesAndSaveInDB(Guid userId, CancellationToken cancellationToken)
    {
        string uploadPath = _hostingInfo.AbsoluteUploadPath();

        string exts = "*.*";

        var files = ScanFiles(_hostingInfo.FileAbsolutePath(MediaDirName), exts, _hostingInfo);
        var existInDbFiles = await _fileRepository.ListAllAbsolutePaths(_hostingInfo, cancellationToken);
        var nonExistFiles = files.Except(existInDbFiles).ToList();

        // зарегистрировать найденные каталоги как папки в БД
        var relPaths = files.Select(f => f.Substring(uploadPath.Length)).ToList();
        var dirPaths = relPaths
            .Where(p => p.Contains('/'))
            .Select(p => p[..p.LastIndexOf('/')])
            .Distinct();
        var pathToFolderId = await _folderService.EnsureFoldersByPaths(dirPaths, userId, cancellationToken);

        var fileEntities = new List<CreateFileQuery>(nonExistFiles.Count);

        foreach (var filepath in nonExistFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            //string absPath = Path.Combine(uploadPath, filepath.TrimStart(Path.DirectorySeparatorChar).TrimStart(Path.AltDirectorySeparatorChar));
            //string absPath = _hostingInfo.FileAbsolutePath(filepath);
            string filename = Path.GetFileName(filepath);
            //string ext = Path.GetExtension(filepath).TrimStart('.');
            //FileInfo fi = new FileInfo(absPath);
            FileInfo fi = new(filepath);

            var filePathFromUpload = filepath.Substring(uploadPath.Length);
            var fileDir = filePathFromUpload.Contains('/') ? filePathFromUpload[..filePathFromUpload.LastIndexOf('/')] : null;

            var createQuery = new CreateFileQuery
            {
                FilePathFromUpload = filePathFromUpload,
                Name = filename,
                Size = (ulong)fi.Length,
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

    async Task<UserActionResult> GenerateThumbnails(bool onlyWithEmptyMeta, CancellationToken cancellationToken)
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
                        _fileStorage.Read(file.FilePhysicalPath, out var fileStream);
                        //using (var fileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (fileStream)
                        {
                            var image = _imageProcessor.ImageSize(fileStream);
                            //file.Meta.ImageInfo.Width = image.Width;
                            //file.Meta.ImageInfo.Height = image.Height;
                            imageMeta = new ImageInfoDto { Width = image.Width, Height = image.Height };
                        }
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
                        //string thumbFilepath = _storage.FullFilePath(file.FilePath);
                        //var thumb = _storage.GetImageThumbnail(cfg, thumbFilepath);
                        //var thumbFilepath = _hostingInfo.FileAbsolutePath(file.FilePhysicalPath);
                        var thumb = GetImageThumbnail(cfg, file.FilePhysicalPath);
                        //file.Meta.Thumbnails.Add(cfg.Name, thumb);
                        thumbnails.Add(cfg.Name, thumb);
                    }
                }
                else
                {
                    foreach (var cfg in thumbOptions)
                    {
                        //string thumbFilepath = _storage.GenerateImageThumbPath(cfg, file.FilePath, file.FileType);
                        var fullFilePath = _hostingInfo.FileAbsolutePath(file.FilePhysicalPath);
                        var filePathFromUpload = file.FilePhysicalPath;
                        string thumbFilepath = GenerateImageThumbPath(cfg, filePathFromUpload);
                        var thumFileDir = _hostingInfo.NormalizePathSlashes(Path.GetDirectoryName(thumbFilepath))!;
                        if (!_fileStorage.DirectoryExists(thumFileDir)) _fileStorage.CreateDirectory(thumFileDir);

                        string thumbFilepathAbsolutePath = _hostingInfo.FileAbsolutePath(thumbFilepath);

                        _imageProcessor.ProcessImage(fullFilePath, thumbFilepathAbsolutePath, cfg);
                        var thumb = GetImageThumbnail(cfg, thumbFilepath);
                        //file.Meta.Thumbnails.Add(cfg.Name, thumb);
                        thumbnails.Add(cfg.Name, thumb);
                    }
                }
                //ef.Files.Update(file);

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
    public List<string> ScanFiles(string path, string pattern, FileHostingInfo hostingInfo)
    {
        string[] ignoreList = { "bin", "obj", ".git", "node_modules" };

        //Directory.GetFileSystemEntries(path, "*.html", SearchOption.AllDirectories);

        var files = FindAllFiles(path, pattern /* "*.*" */, ignoreList, hostingInfo);

        return files;
    }

    public static List<string> FindAllFiles(string rootDir, string pattern, string[] ignoreList, FileHostingInfo hostingInfo)
    {
        var pathsToSearch = new Queue<string>();
        var foundFiles = new List<string>();

        pathsToSearch.Enqueue(rootDir);

        while (pathsToSearch.Count > 0)
        {
            var dir = pathsToSearch.Dequeue();

            try
            {
                var files = Directory.GetFiles(dir, pattern);
                foreach (var file in files)
                {
                    var name = hostingInfo.NormalizePathSlashes(file)!;
                    foundFiles.Add(name);
                }

                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(subDir);
                    if (ignoreList.Contains(name)) continue;
                    pathsToSearch.Enqueue(subDir);
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FindAllFiles: " + ex.Message);
            }
        }

        return foundFiles;
    }
    #endregion
}
