using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Abstractions.Models;

/// <summary>Структура базы → каталог источника: общий вид дерева объектов для всех типов источников.</summary>
public static class CatalogMapping
{
    public static readonly DatasourceCapabilities SqlCapabilities = new()
    {
        CanQuery = true,
        CanBrowse = true,
        CanWrite = true,
        CanManageViews = true,
    };

    public static DatasourceCatalog FromStructure(QDatabaseStructure structure, DatasourceConfig config)
        => new()
        {
            Kind = string.IsNullOrWhiteSpace(config.Kind) ? DatasourceKind.Sql : config.Kind,
            SourceName = structure.DatabaseName,
            Capabilities = SqlCapabilities,
            Groups = structure.Tables
                .GroupBy(table => table.TableSchema?.SchemaName ?? "")
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new DatasourceCatalogGroup
                {
                    Name = group.Key,
                    Objects = group
                        .OrderBy(table => table.TableName, StringComparer.OrdinalIgnoreCase)
                        .Select(table => ToObject(table, group.Key))
                        .ToList(),
                })
                .ToList(),
        };

    public static string ObjectId(string? schemaName, string tableName)
        => string.IsNullOrEmpty(schemaName) ? tableName : $"{schemaName}.{tableName}";

    static DatasourceCatalogObject ToObject(QTable table, string schemaName)
    {
        IEnumerable<QTableColumn> columns = table.Columns?.Values ?? Enumerable.Empty<QTableColumn>();

        return new DatasourceCatalogObject
        {
            Id = ObjectId(schemaName, table.TableName),
            Name = table.TableName,
            ObjectType = table.TableSchema?.Kind ?? QTableKind.Table,
            DefaultLanguage = DatasourceLanguage.Sql,
            Columns = columns
                .OrderBy(column => column.ColumnOrdinal)
                .Select(ToColumn)
                .ToList(),
        };
    }

    static DatasourceCatalogColumn ToColumn(QTableColumn column) => new()
    {
        Name = column.ColumnName,
        Ordinal = column.ColumnOrdinal,
        DataTypeName = column.DataTypeName,
        ClrTypeName = column.DataType?.FullName ?? "",
        Size = column.ColumnSize,
        IsNullable = column.IsNullable,
        IsKey = column.IsKey == true,
        IsJson = column.IsJson,
        IsAutoIncrement = column.IsAutoIncrement == true,
    };
}
