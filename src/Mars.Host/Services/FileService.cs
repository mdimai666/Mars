using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Services;

internal class FileService : IFileService
{
    public const int MaxFileNameSize = 256;
    public const string MediaDirName = "Media";
    public const string MediaThumbsDirName = "MediaThumbs";

    protected readonly IFileStorage _fileStorage;
    protected readonly IOptionService _optionService;
    protected readonly IFileRepository _fileRepository;
    protected readonly IImageProcessor _imageProcessor;

    protected readonly FileHostingInfo _hostingInfo;

    public FileService(
        IFileStorage fileStorage,
        IOptionService optionService,
        IFileRepository fileRepository,
        IImageProcessor imageProcessor)
    {
        _fileStorage = fileStorage;
        _optionService = optionService;
        _fileRepository = fileRepository;
        _imageProcessor = imageProcessor;

        _hostingInfo = _optionService.FileHostingInfo();

        EnsureDirectoriesExist();
    }

    void EnsureDirectoriesExist()
    {
        string[] requiredDirs = [MediaDirName, MediaThumbsDirName];
        foreach (string dir in requiredDirs)
        {
            if (!_fileStorage.DirectoryExists(dir))
            {
                _fileStorage.CreateDirectory(dir);
            }
        }
    }

    public Task<ListDataResult<FileListItem>> List(ListFileQuery query, CancellationToken cancellationToken)
        => _fileRepository.List(query, _hostingInfo, cancellationToken);

    public Task<PagingResult<FileListItem>> ListTable(ListFileQuery query, CancellationToken cancellationToken)
        => _fileRepository.ListTable(query, _hostingInfo, cancellationToken);

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        //delete file
        //delete thumbnails
        var file = await _fileRepository.GetDetail(id, _hostingInfo, cancellationToken);

        await _fileRepository.Delete(id, cancellationToken);

        if (_fileStorage.FileExists(file.FilePhysicalPath))
        {
            _fileStorage.Delete(file.FilePhysicalPath);
        }
        if (file.IsImage && file.Meta is not null)
        {
            if (file.Meta.Thumbnails?.Any() ?? false)
            {
                foreach (var thumb in file.Meta.Thumbnails.Values)
                {
                    _fileStorage.DeleteIfExist(thumb.FilePath);
                }
            }
        }

        return UserActionResult.Success();
    }

    public Task<FileDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _fileRepository.GetDetail(id, _hostingInfo, cancellationToken);

    public Task Update(UpdateFileQuery query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
        => _fileRepository.Update(query, hostingInfo, cancellationToken);
    
    public Task UpdateBulk(IReadOnlyCollection<UpdateFileQuery> query, FileHostingInfo hostingInfo, CancellationToken cancellationToken)
        => _fileRepository.UpdateBulk(query, hostingInfo, cancellationToken);


    /*
    public virtual TFileEntity WriteAvatar(User user, IFormFile file)
    {
        //using Image preview = Image.Thumbnail(user.FullName)

        using (var ms = new MemoryStream())
        {
            file.CopyTo(ms);
            byte[] fileBytes = ms.ToArray();
            //string s = Convert.ToBase64String(fileBytes);
            // act on the Base64 data

            return WriteUpload(file.FileName, EFileType.UserAvatar, fileBytes, user.Id, "avatar");
        }

    }

    public virtual TFileEntity WriteUpload(IFormFile file, EFileType type, string fileGroup, Guid? userId = null)
    {
        using (var ms = new MemoryStream())
        {
            file.CopyTo(ms);
            byte[] fileBytes = ms.ToArray();
            //string s = Convert.ToBase64String(fileBytes);
            // act on the Base64 data

            if (userId == null || Guid.Empty == userId)
            {
                userId = _userId;
            }

            if (userId == null) throw new ArgumentException("User is null");

            return WriteUpload(file.FileName, type, fileBytes, userId.Value, fileGroup);
        }
    }

    public UserActionResult<TFileEntity> WriteUpload<TModel>(TModel model, IFormFile file, EFileType type, string fileGroup, Guid? userId = null)
        where TModel : class, IBasicEntity, new()
    {
        try
        {
            var writedFile = WriteUpload(file, type, fileGroup, userId);

            return new UserActionResult<TFileEntity>
            {
                Ok = true,
                Data = writedFile,
                Message = "Успешно записано",
            };

        }
        catch (Exception ex)
        {
            return new UserActionResult<TFileEntity>
            {
                Ok = false,
                Message = ex.Message
            };
        }
    }
    */


    public Task<Guid> WriteUpload(
        string originalFileNameWithExt,
        string subpath,
        byte[] bytes,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Stream stream = new MemoryStream(bytes);
        return WriteUpload(originalFileNameWithExt, subpath, stream, userId, cancellationToken);
    }

    public Task<Guid> WriteUpload(
        IFormFile formFile,
        string subpath,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var originalFileNameWithExt = string.IsNullOrEmpty(formFile.FileName) ? Guid.NewGuid().ToString() : formFile.FileName;
        return WriteUpload(originalFileNameWithExt, subpath, formFile.OpenReadStream(), userId, cancellationToken);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns>created FileEntity Id</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Guid> WriteUpload(
        string originalFileNameWithExt,
        string subpath,
        Stream fileStream,
        Guid userId,
        //string fileGroup,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty");

        cancellationToken.ThrowIfCancellationRequested();

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileNameWithExt);
        //string filedir = Path.Join(string.IsNullOrEmpty(subpath) ? type.ToString() : subpath);
        string filedir = subpath;

        var hostingInfo = _hostingInfo;
        MediaOption mediaOption = _optionService.GetOption<MediaOption>();

        string ext = hostingInfo.GetExtension(originalFileNameWithExt);
        var isImage = hostingInfo.ExtIsImage(ext);
        var isSvg = hostingInfo.ExtIsSvg(ext);
        var newfilename = RetriveNewFileName(fileNameWithoutExtension, ext, filedir);

        string filepathFromUpload = filedir + '/' + newfilename;
        string fileAbsolutePath = hostingInfo.FileAbsolutePath(filepathFromUpload);

        try
        {
            //check subpath folder
            if (!_fileStorage.DirectoryExists(filedir))
            {
                _fileStorage.CreateDirectory(filedir);
            }

            int? detectImageWidth = null, detectImageHeight = null;

            if (isImage && mediaOption.IsAutoResizeUploadImage && _imageProcessor.IsSupportImageExt(ext))
            {
                using (FileStream fs = new FileStream(fileAbsolutePath, FileMode.CreateNew, FileAccess.Write))
                {
                    var result = _imageProcessor.ProcessImage(fileStream, fs, mediaOption.AutoResizeUploadImageConfig);

                    detectImageWidth ??= result.Width;
                    detectImageHeight ??= result.Height;
                }
            }
            else
            {
                _fileStorage.Write(filepathFromUpload, fileStream);
            }

            var fi = _fileStorage.FileInfo(filepathFromUpload);

            if (isImage && !isSvg && (detectImageWidth is null || detectImageHeight is null))
            {
                try
                {
                    //using (var fileStream = new FileStream(hostingInfo.FileAbsolutePath(filepathFromUpload), FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var image = _imageProcessor.ImageSize(fileStream);
                        detectImageWidth = image.Width;
                        detectImageHeight = image.Height;
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.Error.WriteLine(ex);
#endif
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            string originalFileNameWithExtShort = (fileNameWithoutExtension.Count() < 100 ? fileNameWithoutExtension : fileNameWithoutExtension.Substring(0, 100)) + "." + ext;

            var fileMeta = isImage
                            ? GenerateThumbnailsAndGetFileMeta(filepathFromUpload, ext, mediaOption, detectImageWidth, detectImageHeight)
                            : null;

            var createFileQuery = new CreateFileQuery
            {
                Name = originalFileNameWithExtShort,
                Size = (ulong)fi.Length,
                Meta = fileMeta,
                UserId = userId,
                FilePathFromUpload = filepathFromUpload,
            };

            var createdId = await _fileRepository.Create(createFileQuery, hostingInfo, cancellationToken);

            //return await _fileRepository.GetDetail(createdId, hostingInfo, cancellationToken);

            return createdId;
        }
        catch (Exception)
        {
            _fileStorage.DeleteIfExist(filepathFromUpload);
            throw;
        }
    }

    internal string RetriveNewFileName(string fileNameWithoutExtension, string ext, string filedir)
    {
        string newfilenameAsSlug = Tools.TranslateToPostSlug(fileNameWithoutExtension);
        string newfilename;

        int tryCount = 1;
        newfilename = newfilenameAsSlug + "." + ext;

        var dateStamp = DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");

        var postfixFileNameLength = dateStamp.Length + ext.Length + 1/*dot sumbol*/ + 4/*(tryCount00)*/;

    TryFileName:

        if (_fileStorage.FileExists(Path.Join(filedir, newfilename)))
        {
            if (tryCount > 100) throw new Exception("maximum filename give count");
            tryCount++;
            if (tryCount < 10)
            {
                newfilename = $"{newfilenameAsSlug}({tryCount}).{ext}";
            }
            else
            {
                if (postfixFileNameLength + newfilenameAsSlug.Length > MaxFileNameSize)
                    newfilenameAsSlug = newfilenameAsSlug.Substring(0, MaxFileNameSize - postfixFileNameLength);

                newfilename = $"{newfilenameAsSlug}({dateStamp}).{ext}";
            }
            goto TryFileName;
        }

        return newfilename;
    }

    internal FileEntityMetaDto? GenerateThumbnailsAndGetFileMeta(
        string filePathFromUpload,
        string ext,
        MediaOption mediaOption,
        int? detectImageWidth,
        int? detectImageHeight)
    {
        if (!_hostingInfo.ExtIsImage(ext)) return null;

        string fullFilePath = _hostingInfo.FileAbsolutePath(filePathFromUpload);

        var imageInfo = (detectImageWidth == null || detectImageHeight == null)
            ? null
            : new ImageInfoDto { Width = detectImageWidth.Value, Height = detectImageHeight.Value };

        Dictionary<string, ImageThumbnailDto>? thumbnails;

        if (_hostingInfo.ExtIsSvg(ext))
        {
            thumbnails = new(1);

            foreach (var cfg in mediaOption.ImagePreviewSizeConfigs)
            {
                //string thumbFilepath = _hostingInfo.FileAbsolutePath(filePathFromUpload);
                var thumb = GetImageThumbnail(cfg, filePathFromUpload);
                thumbnails.Add(cfg.Name, thumb);

            }
        }
        else
        {
            thumbnails = new(mediaOption.ImagePreviewSizeConfigs.Length);

            foreach (var cfg in mediaOption.ImagePreviewSizeConfigs)
            {
                string thumbFilepath = GenerateImageThumbPath(cfg, filePathFromUpload);
                string thumbFilepathAbsolutePath = _hostingInfo.FileAbsolutePath(thumbFilepath);
                var result = _imageProcessor.ProcessImage(fullFilePath, thumbFilepathAbsolutePath, cfg);
                var thumb = GetImageThumbnail(cfg, thumbFilepath);
                thumbnails.Add(cfg.Name, thumb);

            }
        }

        return new FileEntityMetaDto
        {
            ImageInfo = imageInfo,
            Thumbnails = thumbnails,
        };
    }

    public string GenerateImageThumbPath(ImagePreviewSizeConfig config, string filePathFromUpload)
    {
        var filename = Path.GetFileNameWithoutExtension(NormalizeAnyPlatformPath(filePathFromUpload));
        var path = Path.GetDirectoryName(filePathFromUpload);
        //return Path.Join(_mediaThumbsPath, path, $"{filename}_{config.Name}.webp");
        //return _hostingInfo.FileAbsolutePath($"{MediaThumbsDirName}/{path}/" + $"{filename}_{config.Name}.webp");
        return $"{MediaThumbsDirName}/{path}/" + $"{filename}_{config.Name}.webp";
    }

    public ImageThumbnailDto GetImageThumbnail(ImagePreviewSizeConfig cfg, string thumbFilepath)
    {
        var thumb = new ImageThumbnailDto
        {
            //FilePath = thumbFilepath.Replace(_uploadPath, ""),
            FilePath = _hostingInfo.NormalizePathSlashes(thumbFilepath)!,
            //FileUrl = thumbFilepath.Replace(wwwRoot, "").Replace("\\", "/"),
            FileUrl = _hostingInfo.FileRelativeUrlFromPath(thumbFilepath),
            Width = cfg.Width,
            Height = cfg.Height,
            Name = cfg.Name
        };

        return thumb;
    }

    public string NormalizeAnyPlatformPath(string path)
    {

        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        if (OperatingSystem.IsWindows())
        {
            return path.Replace('/', '\\');
        }
        else
        {
            return path.Replace('\\', '/');
        }
    }
}
