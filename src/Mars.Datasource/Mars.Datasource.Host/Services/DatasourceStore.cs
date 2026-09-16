using Mars.Datasource.Abstractions.Interfaces;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Host.Services;

internal class DatasourceStore : IDatasourceStore
{
    public const string Root = "datasource";
    public const string DataDirectory = "files";

    readonly IFileStorage _storage;

    public DatasourceStore([FromKeyedServices("data")] IFileStorage storage)
    {
        _storage = storage;
    }

    public IReadOnlyCollection<string> ListDataFiles(string slug)
    {
        var path = DirectoryPath(slug, DataDirectory);

        if (!_storage.DirectoryExists(path)) return [];

        return _storage.GetDirectoryContents(path)
            .Where(entry => !entry.IsDirectory)
            .Select(entry => entry.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool DataFileExists(string slug, string fileName)
        => _storage.FileExists(FilePath(slug, DataDirectory, fileName));

    public Stream OpenDataFile(string slug, string fileName)
        => _storage.OpenRead(FilePath(slug, DataDirectory, fileName));

    public void WriteDataFile(string slug, string fileName, Stream content)
        => _storage.Write(FilePath(slug, DataDirectory, fileName), content);

    static string DirectoryPath(string slug, string directory)
        => $"{Root}/{CheckSlug(slug)}/{CheckRelative(directory)}";

    static string FilePath(string slug, string directory, string fileName)
        => $"{DirectoryPath(slug, directory)}/{CheckRelative(fileName)}";

    static string CheckSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return CheckRelative(slug);
    }

    /// <summary>Хранилище принимает только относительные пути: имя файла не должно выводить за папку источника.</summary>
    static string CheckRelative(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalized = path.Replace('\\', '/').Trim('/');

        if (normalized.Length == 0 || normalized.Split('/').Any(part => part is ".." or ""))
        {
            throw new ArgumentException($"Недопустимый путь \"{path}\"", nameof(path));
        }

        return normalized;
    }
}
