using System.Text;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Host.Services;

/// <summary>
/// Служебные тела источника в data-корне: <c>data/datasource/&lt;slug&gt;/&lt;имя&gt;</c>.
/// </summary>
internal class DatasourceStore : IDatasourceStore
{
    readonly IFileStorage _data;

    public DatasourceStore([FromKeyedServices("data")] IFileStorage data)
    {
        _data = data;
    }

    public bool Exists(string slug, string name) => _data.FileExists(FullPath(slug, name));

    public async Task<string?> ReadTextAsync(string slug, string name, CancellationToken cancellationToken = default)
    {
        var path = FullPath(slug, name);

        if (!_data.FileExists(path)) return null;

        using var stream = _data.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return await reader.ReadToEndAsync(cancellationToken);
    }

    public async Task WriteTextAsync(string slug, string name, string content, CancellationToken cancellationToken = default)
    {
        var path = FullPath(slug, name);

        _data.CreateDirectory("datasource/" + SafeName(slug));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content ?? ""));
        await _data.WriteAsync(path, stream, cancellationToken);
    }

    static string FullPath(string slug, string name) => $"datasource/{SafeName(slug)}/{SafeName(name)}";

    /// <summary>Имя приходит из настроек, поэтому выход за каталог источника исключаем сами.</summary>
    static string SafeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var name = value.Replace('\\', '/').Trim('/');

        if (name.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(part => part is "." or ".."))
        {
            throw new ArgumentException($"Недопустимое имя \"{value}\"", nameof(value));
        }

        return name;
    }
}
