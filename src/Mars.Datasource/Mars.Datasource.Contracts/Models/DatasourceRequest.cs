namespace Mars.Datasource.Contracts.Models;

/// <summary>Тип источника: чем он является и какой у него язык запроса.</summary>
public static class DatasourceKind
{
    public const string Sql = "sql";
    public const string File = "file";
    public const string Rest = "rest";
}

/// <summary>Язык текста в <see cref="DatasourceRequest.Query"/>.</summary>
public static class DatasourceLanguage
{
    public const string Sql = "sql";
    public const string Linq = "linq";

    /// <summary>HTTP-запрос в синтаксисе VS Code REST Client (`.http`).</summary>
    public const string Http = "http";
}

/// <summary>
/// Запрос к источнику. Смысл текста определяет <see cref="Language"/>: для sql это SQL,
/// для linq — выражение Dynamic LINQ. Параметры подставляются провайдером (в SQL — как <c>@name</c>).
/// </summary>
public class DatasourceRequest
{
    /// <summary>Объект или операция из каталога; null — запрос свободным текстом.</summary>
    public string? ObjectId { get; set; }

    public string Language { get; set; } = DatasourceLanguage.Sql;

    public string Query { get; set; } = "";

    public List<DatasourceParam>? Parameters { get; set; }

    /// <summary>Максимум строк результата; 0 — без ограничения.</summary>
    public int MaxRows { get; set; }

    /// <summary>Таймаут запроса в секундах; null — провайдерский по умолчанию.</summary>
    public int? TimeoutSec { get; set; }
}

/// <summary>
/// Параметр запроса. <see cref="Value"/> = null означает SQL NULL.
/// </summary>
public class DatasourceParam
{
    public string Name { get; set; } = "";
    public string? Value { get; set; }
}
