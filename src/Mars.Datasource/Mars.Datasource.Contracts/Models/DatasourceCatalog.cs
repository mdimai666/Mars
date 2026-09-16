namespace Mars.Datasource.Contracts.Models;

/// <summary>
/// Каталог источника: дерево объектов, общее для всех типов источников.
/// У sql-источника группы — схемы, объекты — таблицы и вьюхи; у файлового объекты — файлы и листы;
/// у rest-источника объекты — операции.
/// </summary>
public class DatasourceCatalog
{
    public string Kind { get; set; } = "";

    public string SourceName { get; set; } = "";

    public DatasourceCapabilities Capabilities { get; set; } = new();

    public List<DatasourceCatalogGroup> Groups { get; set; } = [];
}

public class DatasourceCatalogGroup
{
    public string Name { get; set; } = "";

    public List<DatasourceCatalogObject> Objects { get; set; } = [];
}
