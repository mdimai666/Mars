using System.Runtime.InteropServices;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mars.Host.Services;

public class StorageService : IStorageService
{
    protected IHostEnvironment _environment;
    public readonly string _uploadPath;
    public readonly string _mediaThumbsPath;
    public readonly string _mediaPath;
    //IFileProvider contentRoot;
    //readonly ZayavkaService _zayavkaService;

    public readonly string wwwRoot;
    public string ResPath { get; }
    public string WwwRoot => wwwRoot;
    public string UploadPath => _uploadPath;
    public string MediaPath => _mediaPath;
    public string MediaThumbsPath => _mediaPath;


    public StorageService(IConfiguration configuration,
        IServiceProvider serviceProvider,
        IHostEnvironment environment)
    {
        _environment = environment;

        wwwRoot = Path.Join(environment.ContentRootPath, "wwwroot");

        _uploadPath = Path.Join(environment.ContentRootPath, "wwwroot", "upload");
        ResPath = Path.Join(environment.ContentRootPath, "Res");

        _mediaPath = Path.Combine(_uploadPath, EFileType.Media.ToString());
        _mediaThumbsPath = Path.Join(_uploadPath, "MediaThumbs");

        if (!initialCheck)
        {
            CheckFileFolders();
            initialCheck = true;
        }
    }

    static bool initialCheck = false;

    private void CheckFileFolders()
    {
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
        if (!Directory.Exists(Path.Join(_uploadPath, "Temp")))
        {
            Directory.CreateDirectory(Path.Join(_uploadPath, "Temp"));
        }

        if (!Directory.Exists(_mediaThumbsPath))
        {
            Directory.CreateDirectory(_mediaThumbsPath);
        }
        if (!Directory.Exists(_mediaPath))
        {
            Directory.CreateDirectory(_mediaPath);
        }

        foreach (var dir in Enum.GetValues(typeof(EFileType)))
        {
            EFileType e = (EFileType)dir;
            if (e == EFileType.None) continue;

            string path = Path.Join(_uploadPath, e.ToString());

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        foreach (var dir in Enum.GetValues(typeof(EFileType)))
        {
            EFileType e = (EFileType)dir;
            if (e == EFileType.None) continue;

            string path = Path.Join(_mediaThumbsPath, e.ToString());

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    public void WriteUpload(string filepath, byte[] bytes)
    {
        File.WriteAllBytes(Path.Join(_uploadPath, filepath), bytes);
    }

    public void WriteUpload(string filepath, string text)
    {
        File.WriteAllText(Path.Join(_uploadPath, filepath), text);
    }

    private byte[] ReadUploadAllBytes(string filepath)
    {
        return File.ReadAllBytes(Path.Join(_uploadPath, filepath));
    }

    public string ReadUploadAllText(string filepath)
    {
        return File.ReadAllText(Path.Join(_uploadPath, filepath));
    }

    public bool ExistUploadFile(string filepath)
    {
        return File.Exists(Path.Join(_uploadPath, filepath));
    }

    public string FullFilePath(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.GetFullPath(Path.Join(_uploadPath, path.Replace('/', '\\')));
        }
        else
        {
            return Path.GetFullPath(Path.Join(_uploadPath, path.Replace('\\', '/')));
        }
    }

    public bool FileExists(string filepath)
    {
        return File.Exists(Path.Join(_uploadPath, filepath));
    }

    public string GenerateImageThumbPath(ImagePreviewSizeConfig config, string filepath)
    {
        var filename = Path.GetFileNameWithoutExtension(NormalizeAnyPlatformPath(filepath));
        var path = Path.GetDirectoryName(filepath);
        return Path.Join(_mediaThumbsPath, path, $"{filename}_{config.Name}.webp");
    }

    public ImageThumbnailDto GetImageThumbnail(ImagePreviewSizeConfig cfg, string thumbFilepath)
    {
        var thumb = new ImageThumbnailDto
        {
            FilePath = thumbFilepath.Replace(_uploadPath, ""),
            FileUrl = thumbFilepath.Replace(wwwRoot, "").Replace("\\", "/"),
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

    public void Delete(string filepath)
    {
        File.Delete(Path.Join(_uploadPath, filepath));
    }

    public bool DeleteIfExist(string filepath)
    {
        var path = Path.Join(_uploadPath, filepath);
        if (File.Exists(path))
        {
            File.Delete(path);
            return true;
        }
        return false;
    }

    public bool DirectoryExists(string filepath)
    {
        return Directory.Exists(Path.Join(UploadPath, filepath));
    }

    public void CreateDirectory(string filepath)
    {
        Directory.CreateDirectory(Path.Join(UploadPath, filepath));
    }

    public void DeleteDirectory(string filepath, bool recursive)
    {
        Directory.Delete(Path.Join(UploadPath, filepath), recursive);
    }

    public void WriteUpload(string filepath, Stream stream)
    {
        throw new NotImplementedException();
    }
}
