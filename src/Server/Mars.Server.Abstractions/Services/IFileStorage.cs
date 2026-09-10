using Microsoft.Extensions.FileProviders;

namespace Mars.Server.Abstractions.Services;

/// <summary>
/// Дает доступ к файлам на локальном или удаленном хранилище.
/// Пути относительны корня хранилища, разделитель — '/', абсолютные пути и выход за корень запрещены.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Открывает файл на чтение. Поток доступен только для чтения, вызывающий обязан его освободить
    /// </summary>
    /// <exception cref="FileNotFoundException">файл не найден</exception>
    Stream OpenRead(string filepath);

    void Write(string filepath, Stream stream);
    Task WriteAsync(string filepath, Stream stream, CancellationToken cancellationToken);

    bool FileExists(string filepath);

    /// <summary>
    /// Удаляет файл, если он существует. Возвращает true, если файл был удален
    /// </summary>
    bool DeleteFile(string filepath);

    IDirectoryContents GetDirectoryContents(string subpath);

    /// <summary>
    /// Возвращает файл или каталог, либо null если ничего не найдено
    /// </summary>
    IFileInfo? GetFileInfo(string filepath);

    void CreateDirectory(string filepath);
    bool DirectoryExists(string filepath);
    void DeleteDirectory(string path, bool recursive);

    /// <summary>
    /// Перемещает файл на новый путь
    /// </summary>
    void MoveFile(string fromPath, string toPath);

    /// <summary>
    /// Перемещает каталог со всем содержимым на новый путь
    /// </summary>
    void MoveDirectory(string fromPath, string toPath);
}
