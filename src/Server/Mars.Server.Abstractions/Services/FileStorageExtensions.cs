using System.Text;

namespace Mars.Server.Abstractions.Services;

/// <summary>
/// Удобные обертки над примитивами <see cref="IFileStorage"/>.
/// Реализации хранилища обязаны уметь только примитивы — сахар не дублируется в каждом провайдере
/// </summary>
public static class FileStorageExtensions
{
    public static string ReadAllText(this IFileStorage storage, string filepath)
    {
        using var stream = storage.OpenRead(filepath);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static byte[] ReadAllBytes(this IFileStorage storage, string filepath)
    {
        using var stream = storage.OpenRead(filepath);
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static void Write(this IFileStorage storage, string filepath, byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes, writable: false);
        storage.Write(filepath, memoryStream);
    }

    public static void Write(this IFileStorage storage, string filepath, string text)
    {
        storage.Write(filepath, Encoding.UTF8.GetBytes(text));
    }
}
