namespace Mars.Datasource.Abstractions.Interfaces;

/// <summary>
/// Файлы источника в data-корне (`data/datasource/&lt;slug&gt;/…`): данные внешних таблиц, а позже —
/// документ запросов и каталог discovery. Большие тела не живут в опциях: опция сериализуется целиком
/// на каждое сохранение и уезжает в админку, поэтому в конфиге источника держат только имена и ссылки.
/// </summary>
public interface IDatasourceStore
{
    /// <summary>Имена файлов с данными источника (без пути), отсортированные; пустой список, если папки нет.</summary>
    IReadOnlyCollection<string> ListDataFiles(string slug);

    bool DataFileExists(string slug, string fileName);

    /// <summary>Поток только на чтение; освобождает вызывающий.</summary>
    Stream OpenDataFile(string slug, string fileName);

    void WriteDataFile(string slug, string fileName, Stream content);
}
