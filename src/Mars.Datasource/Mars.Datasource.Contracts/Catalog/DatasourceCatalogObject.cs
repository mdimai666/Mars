namespace Mars.Datasource.Contracts.Catalog;

public class DatasourceCatalogObject
{
    /// <summary>Идентификатор объекта в пределах источника: у sql — <c>schema.table</c>, у rest — имя запроса.</summary>
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string ObjectType { get; set; } = DatasourceObjectType.Table;

    public List<DatasourceField> Fields { get; set; } = [];

    /// <summary>Язык запроса по умолчанию для этого объекта.</summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>Текст, который подставляется в редактор при открытии объекта.</summary>
    public string? DefaultQuery { get; set; }

    /// <summary>Вызываемая операция (rest и другие источники с каталогом операций); null — объект данных.</summary>
    public DatasourceOperation? Operation { get; set; }
}

/// <summary>
/// Операция каталога: чем её вызывать, какие у неё параметры и где лежит её текст
/// в документе запросов пользователя.
/// </summary>
public class DatasourceOperation
{
    /// <summary>Способ вызова: у rest — HTTP-метод; у источников без метода пусто.</summary>
    public string Method { get; set; } = "";

    public List<DatasourceOperationParameter> Parameters { get; set; } = [];

    /// <summary>
    /// Строки документа запросов, где лежит операция: дерево переходит к блоку,
    /// «выполнить» берёт блок под курсором. 0 — в документе операции нет.
    /// </summary>
    public int Line { get; set; }

    /// <summary>Последняя строка блока в документе; 0 — операции в документе нет.</summary>
    public int EndLine { get; set; }
}

public static class DatasourceObjectType
{
    public const string Table = "table";
    public const string View = "view";
    public const string MaterializedView = "matview";
    public const string File = "file";
    public const string Operation = "operation";
}
