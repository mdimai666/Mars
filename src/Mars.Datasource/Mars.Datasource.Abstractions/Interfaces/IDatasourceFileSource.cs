namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Чтение файла источника по ссылке. Ссылка — либо путь в медиа-хранилище Mars (относительный,
/// разделитель '/'), либо абсолютный путь на хосте. В data-корень файлы источников не копируются:
/// они лежат там, где их положил пользователь.
/// </summary>
public interface IDatasourceFileSource
{
    bool Exists(string reference);

    /// <summary>Поток только на чтение; освобождает вызывающий.</summary>
    Stream OpenRead(string reference);
}
