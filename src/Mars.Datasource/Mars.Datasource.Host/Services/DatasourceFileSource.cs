using Mars.Datasource.Abstractions.Interfaces;
using Mars.Server.Abstractions.Services;

namespace Mars.Datasource.Host.Services;

/// <summary>
/// Ссылка на файл источника: относительный путь ищем в медиа-хранилище Mars, корневой — на диске хоста.
/// Корневой путь, которого на диске нет, тоже пробуем как медиа-путь: в форму путь копируют из медиа,
/// а там он бывает с ведущим слэшем.
/// </summary>
internal class DatasourceFileSource : IDatasourceFileSource
{
    readonly IFileStorage _media;

    public DatasourceFileSource(IFileStorage media)
    {
        _media = media;
    }

    public bool Exists(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        if (!Path.IsPathRooted(reference)) return _media.FileExists(MediaPath(reference));

        return File.Exists(reference) || _media.FileExists(MediaPath(reference));
    }

    public Stream OpenRead(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        if (Path.IsPathRooted(reference) && File.Exists(reference)) return File.OpenRead(reference);

        return _media.OpenRead(MediaPath(reference));
    }

    /// <summary>Медиа-хранилище принимает только относительные пути с '/'.</summary>
    static string MediaPath(string reference) => reference.Replace('\\', '/').Trim('/');
}
