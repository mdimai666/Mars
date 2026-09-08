using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using Mars.Contracts.Dto.Files;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.FileProviders;

namespace Mars.Storage.Services;

/// <summary>
/// In-memory реализация <see cref="IFileStorage"/> для тестов.
/// Семантика каталогов и листинга повторяет дисковую <see cref="FileStorage"/>:
/// листинг отдает только непосредственных детей, каталог существует вместе со всеми родительскими сегментами
/// </summary>
public class InMemoryFileStorage : IFileStorage
{
    internal readonly ConcurrentDictionary<string, byte[]> _files = new();
    internal readonly ConcurrentDictionary<string, byte> _directories = new();
    private readonly object _sync = new();

    public InMemoryFileStorage()
    {

    }

    public InMemoryFileStorage(IDictionary<string, string> files)
    {
        foreach (var file in files)
        {
            var filepath = Normalize(file.Key);
            _files[filepath] = Encoding.UTF8.GetBytes(file.Value);
            AddDirectories(DirectoryOf(filepath));
        }
    }

    static string Normalize(string path) => FileHostingInfo.NormalizePathSlash(path)!;

    static string DirectoryOf(string filepath)
    {
        var index = filepath.LastIndexOf('/');
        return index < 0 ? string.Empty : filepath[..index];
    }

    void AddDirectories(string directoryPath)
    {
        if (directoryPath.Length == 0) return;

        var current = string.Empty;
        foreach (var segment in directoryPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current.Length == 0 ? segment : current + '/' + segment;
            _directories[current] = 0;
        }
    }

    public Stream OpenRead(string filepath)
    {
        filepath = Normalize(filepath);
        if (!_files.TryGetValue(filepath, out var bytes))
        {
            throw new FileNotFoundException($"Файл не найден: {filepath}");
        }

        return new MemoryStream(bytes, writable: false);
    }

    public void Write(string filepath, Stream stream)
    {
        filepath = Normalize(filepath);

        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            _files[filepath] = memoryStream.ToArray();
        }

        AddDirectories(DirectoryOf(filepath));
    }

    public async Task WriteAsync(string filepath, Stream stream, CancellationToken cancellationToken)
    {
        filepath = Normalize(filepath);

        using (var memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream, cancellationToken);
            _files[filepath] = memoryStream.ToArray();
        }

        AddDirectories(DirectoryOf(filepath));
    }

    public bool FileExists(string filepath)
    {
        filepath = Normalize(filepath);
        return _files.ContainsKey(filepath);
    }

    public bool DeleteFile(string filepath)
    {
        filepath = Normalize(filepath);
        return _files.TryRemove(filepath, out _);
    }

    public void CreateDirectory(string directoryPath)
    {
        AddDirectories(Normalize(directoryPath));
    }

    public bool DirectoryExists(string directoryPath)
    {
        directoryPath = Normalize(directoryPath);
        return _directories.ContainsKey(directoryPath);
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        path = Normalize(path);
        if (!_directories.ContainsKey(path)) return;

        var prefix = path + '/';

        lock (_sync)
        {
            var childDirectories = _directories.Keys.Where(d => d.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            var childFiles = _files.Keys.Where(f => f.StartsWith(prefix, StringComparison.Ordinal)).ToList();

            if (!recursive && (childDirectories.Count > 0 || childFiles.Count > 0))
            {
                throw new IOException($"Каталог не пуст: {path}");
            }

            foreach (var directory in childDirectories)
            {
                _directories.TryRemove(directory, out _);
            }

            foreach (var file in childFiles)
            {
                _files.TryRemove(file, out _);
            }

            _directories.TryRemove(path, out _);
        }
    }

    public void MoveFile(string fromPath, string toPath)
    {
        fromPath = Normalize(fromPath);
        toPath = Normalize(toPath);

        if (!_files.TryRemove(fromPath, out var bytes))
        {
            throw new FileNotFoundException($"Файл не найден: {fromPath}");
        }

        _files[toPath] = bytes;
        AddDirectories(DirectoryOf(toPath));
    }

    public void MoveDirectory(string fromPath, string toPath)
    {
        fromPath = Normalize(fromPath);
        toPath = Normalize(toPath);

        var prefix = fromPath + '/';

        lock (_sync)
        {
            foreach (var directory in _directories.Keys.Where(d => d == fromPath || d.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                _directories.TryRemove(directory, out _);
                _directories[toPath + directory[fromPath.Length..]] = 0;
            }

            foreach (var file in _files.Keys.Where(f => f.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                if (_files.TryRemove(file, out var bytes))
                {
                    _files[toPath + file[fromPath.Length..]] = bytes;
                }
            }
        }
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        subpath = Normalize(subpath);

        var directories = _directories.Keys
                                      .Where(directory => DirectoryOf(directory) == subpath)
                                      .OrderBy(directory => directory, StringComparer.Ordinal)
                                      .Select(directory => new InMemoryDirectoryContents.FileSystemDirectoryInfo(directory))
                                      .ToList();

        var files = _files.Keys
                          .Where(filepath => DirectoryOf(filepath) == subpath)
                          .OrderBy(filepath => filepath, StringComparer.Ordinal)
                          .Select(filepath => new InMemoryDirectoryContents.FileSystemFileInfo(filepath, this))
                          .ToList();

        return new InMemoryDirectoryContents(files, directories);
    }

    public IFileInfo? GetFileInfo(string filepath)
    {
        filepath = Normalize(filepath);
        if (_files.ContainsKey(filepath))
        {
            return new InMemoryDirectoryContents.FileSystemFileInfo(filepath, this);
        }

        if (_directories.ContainsKey(filepath))
        {
            return new InMemoryDirectoryContents.FileSystemDirectoryInfo(filepath);
        }

        return null;
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

        static string Normalize(string path) => FileHostingInfo.NormalizePathSlash(path)!;

        public bool Exists => _inMemoryFileStorage._files.ContainsKey(Normalize(_filepath));

        public long Length => _inMemoryFileStorage._files.TryGetValue(Normalize(_filepath), out var bytes) ? bytes.Length : 0;
        public string PhysicalPath => _filepath;
        public string Name => Path.GetFileName(_filepath);
        public DateTimeOffset LastModified => DateTimeOffset.Now;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return _inMemoryFileStorage.OpenRead(_filepath);
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
