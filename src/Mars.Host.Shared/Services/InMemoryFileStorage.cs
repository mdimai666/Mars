using System.Collections;
using System.Text;
using Mars.Host.Shared.Dto.Files;
using Microsoft.Extensions.FileProviders;

namespace Mars.Host.Shared.Services;

public class InMemoryFileStorage : IFileStorage
{
    internal readonly Dictionary<string, byte[]> _files = [];
    internal readonly HashSet<string> _directories = [];

    public InMemoryFileStorage()
    {

    }

    string Normalize(string path) => FileHostingInfo.NormalizePathSlash(path)!;

    public InMemoryFileStorage(IDictionary<string, string> files)
    {
        _files = files.ToDictionary(s => s.Key, s => Encoding.UTF8.GetBytes(s.Value));
    }

    public string ReadAllText(string filepath)
    {
        filepath = Normalize(filepath);
        if (!_files.ContainsKey(filepath))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        var bytes = _files[filepath];
        return Encoding.UTF8.GetString(bytes);
    }

    public byte[] Read(string filepath)
    {
        filepath = Normalize(filepath);
        if (!_files.ContainsKey(filepath))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        return (_files[filepath].Clone() as byte[])!;
    }

    public void Read(string filepath, out Stream stream)
    {
        filepath = Normalize(filepath);
        if (!_files.ContainsKey(filepath))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        var bytes = _files[filepath];
        stream = new MemoryStream(bytes);
    }

    public void Write(string filepath, byte[] bytes)
    {
        filepath = Normalize(filepath);
        _files[filepath] = (bytes.Clone() as byte[])!;
    }

    public void Write(string filepath, string text)
    {
        filepath = Normalize(filepath);
        var bytes = Encoding.UTF8.GetBytes(text);
        _files[filepath] = bytes;
    }

    public void Write(string filepath, Stream stream)
    {
        filepath = Normalize(filepath);
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            _files[filepath] = memoryStream.ToArray();
        }
    }

    public Task WriteAsync(string filepath, Stream stream, CancellationToken cancellationToken)
    {
        filepath = Normalize(filepath);
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            _files[filepath] = memoryStream.ToArray();
        }
        return Task.CompletedTask;
    }

    public bool FileExists(string filepath)
    {
        filepath = Normalize(filepath);
        return _files.ContainsKey(filepath);
    }

    public void Delete(string filepath)
    {
        filepath = Normalize(filepath);
        if (_files.ContainsKey(filepath))
        {
            _files.Remove(filepath);
        }
    }

    public bool DeleteIfExist(string filepath)
    {
        filepath = Normalize(filepath);
        if (_files.ContainsKey(filepath))
        {
            _files.Remove(filepath);
            return true;
        }

        return false;
    }

    public void CreateDirectory(string directoryPath)
    {
        directoryPath = Normalize(directoryPath);
        _directories.Add(directoryPath);
    }

    public bool DirectoryExists(string directoryPath)
    {
        directoryPath = Normalize(directoryPath);
        return _directories.Contains(directoryPath);
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        path = Normalize(path);
        if (_directories.Contains(path))
        {
            _directories.Remove(path);

            if (recursive)
            {
                foreach (var key in _files.Keys.ToList())
                {
                    if (key.StartsWith(path))
                    {
                        _files.Remove(key);
                    }
                }
            }
        }
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        subpath = Normalize(subpath);
        var files = new List<InMemoryDirectoryContents.FileSystemFileInfo>();
        var directories = new List<InMemoryDirectoryContents.FileSystemDirectoryInfo>();

        foreach (var dir in _directories)
        {
            if (dir.StartsWith(subpath))
            {
                directories.Add(new InMemoryDirectoryContents.FileSystemDirectoryInfo(dir));
            }
        }

        foreach (var file in _files.Keys)
        {
            if (file.StartsWith(subpath))
            {
                files.Add(new InMemoryDirectoryContents.FileSystemFileInfo(file, this));
            }
        }

        return new InMemoryDirectoryContents(files, directories);
    }

    public IFileInfo FileInfo(string filepath)
    {
        filepath = Normalize(filepath);
        if (_files.ContainsKey(filepath))
        {
            return new InMemoryDirectoryContents.FileSystemFileInfo(filepath, this);
        }
        else if (_directories.Contains(filepath))
        {
            return new InMemoryDirectoryContents.FileSystemDirectoryInfo(filepath);
        }

        return null!;
    }

}

public class InMemoryDirectoryContents : IDirectoryContents
{
    private readonly IEnumerable<IFileInfo> _contents;

    public InMemoryDirectoryContents(List<FileSystemFileInfo> files, List<FileSystemDirectoryInfo> directories)
    {
        _contents = directories.Cast<IFileInfo>().Concat(files);
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
        private readonly string _filepath;
        private readonly InMemoryFileStorage _inMemoryFileStorage;

        public FileSystemFileInfo(string filepath, InMemoryFileStorage inMemoryFileStorage)
        {
            _filepath = filepath;
            _inMemoryFileStorage = inMemoryFileStorage;
        }

        string Normalize(string path) => FileHostingInfo.NormalizePathSlash(path)!;

        public bool Exists => true;

        public long Length => _inMemoryFileStorage._files.TryGetValue(Normalize(_filepath), out var bytes) ? bytes.Length : 0;
        public string PhysicalPath => _filepath;
        public string Name => Path.GetFileName(_filepath);
        public DateTimeOffset LastModified => DateTimeOffset.Now;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            _inMemoryFileStorage.Read(_filepath, out var stream);
            return stream;
        }
    }

    public class FileSystemDirectoryInfo : IFileInfo
    {
        private readonly string _directoryPath;

        public FileSystemDirectoryInfo(string directoryPath)
        {
            _directoryPath = directoryPath;
        }

        public bool Exists => true;
        public long Length => -1;
        public string PhysicalPath => _directoryPath;
        public string Name => Path.GetFileName(_directoryPath);
        public DateTimeOffset LastModified => DateTimeOffset.Now;
        public bool IsDirectory => true;

        public Stream CreateReadStream()
        {
            throw new NotImplementedException();
        }
    }
}
