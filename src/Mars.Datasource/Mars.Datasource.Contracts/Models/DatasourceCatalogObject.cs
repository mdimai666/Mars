namespace Mars.Datasource.Contracts.Models;

public class DatasourceCatalogObject
{
    /// <summary>Идентификатор объекта в пределах источника: у sql — <c>schema.table</c>, у rest — имя запроса.</summary>
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string ObjectType { get; set; } = DatasourceObjectType.Table;

    public List<DatasourceCatalogColumn> Columns { get; set; } = [];

    /// <summary>Параметры операции (rest); у табличных объектов пусто.</summary>
    public List<DatasourceOperationParameter> Parameters { get; set; } = [];

    /// <summary>Язык запроса по умолчанию для этого объекта.</summary>
    public string? DefaultLanguage { get; set; }

    /// <summary>Текст, который подставляется в редактор при открытии объекта.</summary>
    public string? DefaultQuery { get; set; }
}

public static class DatasourceObjectType
{
    public const string Table = "table";
    public const string View = "view";
    public const string MaterializedView = "matview";
    public const string File = "file";
    public const string Operation = "operation";
}
