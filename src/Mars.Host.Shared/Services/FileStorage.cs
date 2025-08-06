using System.Collections;
using Mars.Host.Shared.Dto.Files;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Mars.Host.Shared.Services;

public class FileStorage : IFileStorage
{
    private readonly FileHostingInfo _hostingInfo;

    public FileStorage(IOptions<FileHostingInfo> hostingInfo)
    {
        _hostingInfo = hostingInfo.Value;
    }

    public string ReadAllText(string filepath)
    {
        filepath = AbsolutePath(filepath);

        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        return File.ReadAllText(filepath);
    }

    public byte[] Read(string filepath)
    {
        filepath = AbsolutePath(filepath);

        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        return File.ReadAllBytes(filepath);
    }

    public void Read(string filepath, out Stream stream)
    {
        filepath = AbsolutePath(filepath);

        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        stream = File.OpenRead(filepath);
    }

    public void Write(string filepath, byte[] bytes)
    {
        filepath = AbsolutePath(filepath);
        File.WriteAllBytes(filepath, bytes);
    }

    public void Write(string filepath, string text)
    {
        filepath = AbsolutePath(filepath);
        File.WriteAllText(filepath, text);
    }

    public void Write(string filepath, Stream stream)
    {
        filepath = AbsolutePath(filepath);
        using (var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
        {
            stream.CopyTo(fileStream);
        }
    }

    public async Task WriteAsync(string filepath, Stream stream, CancellationToken cancellationToken)
    {
        filepath = AbsolutePath(filepath);
        using var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);
    }

    public bool FileExists(string filepath)
    {
        filepath = AbsolutePath(filepath);
        return File.Exists(filepath);
    }

    public void Delete(string filepath)
    {
        filepath = AbsolutePath(filepath);
        if (File.Exists(filepath))
        {
            File.Delete(filepath);
        }
    }

    public bool DeleteIfExist(string filepath)
    {
        filepath = AbsolutePath(filepath);
        if (File.Exists(filepath))
        {
            File.Delete(filepath);
            return true;
        }

        return false;
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        subpath = AbsolutePath(subpath);
        return new FileSystemDirectoryContents(subpath);
    }

    public IFileInfo FileInfo(string filepath)
    {
        filepath = AbsolutePath(filepath);
        if (File.Exists(filepath))
        {
            return new FileSystemDirectoryContents.FileSystemFileInfo(filepath);
        }
        else if (Directory.Exists(filepath))
        {
            return new FileSystemDirectoryContents.FileSystemDirectoryInfo(filepath);
        }

        return null!;
    }

    public void CreateDirectory(string directoryPath)
    {
        directoryPath = AbsolutePath(directoryPath);
        Directory.CreateDirectory(directoryPath);
    }

    public bool DirectoryExists(string directoryPath)
    {
        directoryPath = AbsolutePath(directoryPath);
        return Directory.Exists(directoryPath);
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        path = AbsolutePath(path);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive);
        }
    }

    internal string AbsolutePath(string path)
    {
        if (Path.IsPathFullyQualified(path)) throw new Exception("path must be relative");

        return _hostingInfo.FileAbsolutePath(path);
    }
}

public class FileSystemDirectoryContents : IDirectoryContents
{
    private readonly IEnumerable<IFileInfo> _contents;

    public FileSystemDirectoryContents(string subpath)
    {
        if (subpath == null)
        {
            throw new ArgumentNullException(nameof(subpath));
        }

        //var fullPath = Path.Combine(Directory.GetCurrentDirectory(), subpath);
        var fullPath = subpath;
        var files = Directory.EnumerateFiles(fullPath);
        var directories = Directory.EnumerateDirectories(fullPath);

        _contents = files.Select(f => (IFileInfo)new FileSystemFileInfo(f))
                         .Concat(directories.Select(d => new FileSystemDirectoryInfo(d)))
                         .OrderBy(c => c.IsDirectory ? 0 : 1)
                         .ThenBy(c => c.Name);
    }

    public bool Exists => _contents != null && _contents.Any();

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        return _contents.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public class FileSystemFileInfo : IFileInfo
    {
        private readonly FileInfo _fileInfo;

        public FileSystemFileInfo(string path)
        {
            _fileInfo = new FileInfo(path);
        }

        public bool Exists => _fileInfo.Exists;

        public long Length => _fileInfo.Length;

        public string PhysicalPath => _fileInfo.FullName;

        public string Name => _fileInfo.Name;

        public DateTimeOffset LastModified => _fileInfo.LastWriteTimeUtc;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            if (_fileInfo == null || !_fileInfo.Exists)
            {
                throw new FileNotFoundException($"Файл не найден: {_fileInfo?.FullName}", _fileInfo?.FullName);
            }

            return _fileInfo.OpenRead();

        }
    }

    public class FileSystemDirectoryInfo : IFileInfo
    {
        private readonly DirectoryInfo _directoryInfo;

        public FileSystemDirectoryInfo(string path)
        {
            _directoryInfo = new DirectoryInfo(path);
        }

        public bool Exists => _directoryInfo.Exists;

        public long Length => -1;

        public string PhysicalPath => _directoryInfo.FullName;

        public string Name => _directoryInfo.Name;

        public DateTimeOffset LastModified => _directoryInfo.LastWriteTimeUtc;

        public bool IsDirectory => true;

        public Stream CreateReadStream()
        {
            throw new NotImplementedException();
        }
    }
}
