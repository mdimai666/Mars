using System.Data.Common;

namespace Mars.Datasource.Abstractions.Models;

/// <summary>Метаданные таблицы/вьюхи из системного каталога.</summary>
public class QTableMeta
{
    public string SchemaName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string TableOwner { get; set; } = "";
    public string Kind { get; set; } = QTableKind.Table;
}

/// <summary>Метаданные колонки из системного каталога.</summary>
public class QColumnMeta
{
    public string SchemaName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public int ColumnOrdinal { get; set; }
    public int? ColumnSize { get; set; }
    public string DataTypeName { get; set; } = "";
    public bool IsNullable { get; set; } = true;
    public bool IsKey { get; set; }
}

/// <summary>
/// Сборка структуры базы из плоских метаданных — общая для всех провайдеров,
/// чтобы каждая БД запрашивала схему одним-двумя запросами вместо обхода таблиц.
/// </summary>
public static class QDatabaseStructureBuilder
{
    /// <summary>
    /// Порядок колонок выборки метаданных, на который рассчитаны <see cref="ReadTable"/> и <see cref="ReadColumn"/>:
    /// schema, table, (column), ordinal, data_type_name, column_size, is_nullable, is_key.
    /// </summary>
    public static QTableMeta ReadTable(DbDataReader reader) => new()
    {
        SchemaName = reader.GetString(0),
        TableName = reader.GetString(1),
        TableOwner = reader.GetString(2),
        Kind = reader.GetString(3),
    };

    public static QColumnMeta ReadColumn(DbDataReader reader) => new()
    {
        SchemaName = reader.GetString(0),
        TableName = reader.GetString(1),
        ColumnName = reader.GetString(2),
        ColumnOrdinal = Convert.ToInt32(reader.GetValue(3)),
        DataTypeName = reader.GetString(4),
        ColumnSize = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5)),
        IsNullable = Convert.ToBoolean(reader.GetValue(6)),
        IsKey = Convert.ToBoolean(reader.GetValue(7)),
    };

    public static QTableSchema ToSchema(QTableMeta meta) => new()
    {
        SchemaName = meta.SchemaName,
        TableName = meta.TableName,
        TableOwner = meta.TableOwner,
        Kind = meta.Kind,
    };

    public static QTableColumn ToColumn(QColumnMeta meta) => new()
    {
        ColumnName = meta.ColumnName,
        ColumnOrdinal = meta.ColumnOrdinal,
        ColumnSize = meta.ColumnSize,
        IsNullable = meta.IsNullable,
        IsKey = meta.IsKey,
        DataTypeName = meta.DataTypeName,
        DataType = QColumnMapping.ClrType(meta.DataTypeName),
    };

    public static QDatabaseStructure Assemble(string databaseName, IEnumerable<QTableMeta> tables, IEnumerable<QColumnMeta> columns)
    {
        var columnsByTable = columns
            .GroupBy(c => (c.SchemaName, c.TableName))
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.ColumnOrdinal).ToList());

        QDatabaseStructure structure = new() { DatabaseName = databaseName };

        foreach (var table in tables)
        {
            columnsByTable.TryGetValue((table.SchemaName, table.TableName), out var tableColumns);

            structure.Tables.Add(new QTable
            {
                TableName = table.TableName,
                TableSchema = ToSchema(table),
                Columns = (tableColumns ?? []).ToDictionary(c => c.ColumnName, ToColumn),
            });
        }

        return structure;
    }
}
