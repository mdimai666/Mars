using System.Collections;
using Mars.Contracts.Dto.Files;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Mars.Storage.Services;

public class FileStorage : IFileStorage
{
    private readonly FileHostingInfo _hostingInfo;

    public FileStorage(IOptions<FileHostingInfo> hostingInfo)
    {
        _hostingInfo = hostingInfo.Value;
    }

    public Stream OpenRead(string filepath)
    {
        filepath = AbsolutePath(filepath);

        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        return File.OpenRead(filepath);
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

    public bool DeleteFile(string filepath)
    {
        filepath = AbsolutePath(filepath);

        if (!File.Exists(filepath)) return false;

        File.Delete(filepath);
        return true;
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        subpath = AbsolutePath(subpath);
        return new FileSystemDirectoryContents(subpath);
    }

    public IFileInfo? GetFileInfo(string filepath)
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

        return null;
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

    public void MoveFile(string fromPath, string toPath)
    {
        fromPath = AbsolutePath(fromPath);
        toPath = AbsolutePath(toPath);
        File.Move(fromPath, toPath);
    }

    public void MoveDirectory(string fromPath, string toPath)
    {
        fromPath = AbsolutePath(fromPath);
        toPath = AbsolutePath(toPath);
        Directory.Move(fromPath, toPath);
    }

    internal string AbsolutePath(string path)
    {
        if (Path.IsPathFullyQualified(path)) throw new ArgumentException("path must be relative", nameof(path));

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
