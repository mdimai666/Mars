namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Служебные тела источника в data-корне Mars (<c>data/datasource/&lt;slug&gt;/…</c>): документ запросов
/// пользователя и каталог discovery. В опциях на них только ссылка — большое в опцию не кладём.
/// Файлы источников здесь не живут: они в медиа-хранилище или на диске хоста
/// (<see cref="IDatasourceFileSource"/>).
/// </summary>
public interface IDatasourceStore
{
    bool Exists(string slug, string name);

    /// <summary>Содержимое файла или null, если его нет.</summary>
    Task<string?> ReadTextAsync(string slug, string name, CancellationToken cancellationToken = default);

    Task WriteTextAsync(string slug, string name, string content, CancellationToken cancellationToken = default);
}
