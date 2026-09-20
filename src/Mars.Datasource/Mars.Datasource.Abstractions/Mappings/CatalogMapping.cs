using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Abstractions.Mappings;

/// <summary>Структура базы → каталог источника: общий вид дерева объектов для всех типов источников.</summary>
public static class CatalogMapping
{
    public static DatasourceCatalog FromStructure(QDatabaseStructure structure, DatasourceConfig config, DatasourceKindProfile profile)
        => new()
        {
            SourceName = structure.DatabaseName,
            Profile = profile,
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
            Fields = columns
                .OrderBy(column => column.ColumnOrdinal)
                .Select(ToField)
                .ToList(),
        };
    }

    static DatasourceField ToField(QTableColumn column) => new()
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
