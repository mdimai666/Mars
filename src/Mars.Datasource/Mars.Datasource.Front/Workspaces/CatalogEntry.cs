using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Front.Workspaces;

/// <summary>Объект каталога вместе с его группой: у sql группа — схема, она нужна для SQL и DDL вьюх.</summary>
public record CatalogEntry(string Group, DatasourceCatalogObject Object);
