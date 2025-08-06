using Microsoft.Extensions.FileProviders;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Дает доступ к файлам на локальном или удаленном хранилище
/// </summary>
public interface IFileStorage
{
    string ReadAllText(string filepath);
    byte[] Read(string filepath);
    void Read(string filepath, out Stream stream);

    void Write(string filepath, byte[] bytes);
    void Write(string filepath, string text);
    void Write(string filepath, Stream stream);
    Task WriteAsync(string filepath, Stream stream, CancellationToken cancellationToken);

    bool FileExists(string filepath);

    void Delete(string filepath);
    bool DeleteIfExist(string filepath);

    IDirectoryContents GetDirectoryContents(string subpath);
    IFileInfo FileInfo(string filepath);

    void CreateDirectory(string filepath);
    bool DirectoryExists(string filepath);
    void DeleteDirectory(string path, bool recursive);
}
