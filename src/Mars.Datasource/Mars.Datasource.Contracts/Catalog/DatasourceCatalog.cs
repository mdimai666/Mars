namespace Mars.Datasource.Contracts.Catalog;

/// <summary>
/// Каталог источника: дерево объектов, общее для всех типов источников.
/// У sql-источника группы — схемы, объекты — таблицы и вьюхи; у файлового объекты — файлы и листы;
/// у rest-источника объекты — операции.
/// </summary>
public class DatasourceCatalog
{
    public string SourceName { get; set; } = "";

    /// <summary>Профиль типа источника: по нему рабочая область решает, что показывать и чем рисовать.</summary>
    public DatasourceKindProfile Profile { get; set; } = new();

    /// <summary>
    /// Действия источника (утилиты): что провайдер и хост умеют выполнять по идентификатору
    /// через <c>ExecuteAction</c>. Кнопки в UI рисуются из этого списка, а не хардкодятся.
    /// </summary>
    public List<DatasourceActionDescriptor> Actions { get; set; } = [];

    public List<DatasourceCatalogGroup> Groups { get; set; } = [];
}

public class DatasourceCatalogGroup
{
    public string Name { get; set; } = "";

    public List<DatasourceCatalogObject> Objects { get; set; } = [];
}
