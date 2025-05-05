using Mars.Datasource.Core.Dto;

namespace Mars.Datasource.Core.Mappings;

public static class DataSourceMapping
{
    public static QDatabaseStructureResponse ToResponse(this QDatabaseStructure entity)
      => new()
      {
          DatabaseName = entity.DatabaseName,
          Tables = entity.Tables.ToResponse(),
      };

    public static QTableResponse ToResponse(this QTable entity)
      => new()
      {
          TableName = entity.TableName,
          TableSchema = entity.TableSchema.ToResponse(),
          Columns = entity.Columns.ToResponse(),
      };
    public static QTableColumnResponse ToResponse(this QTableColumn entity)
      => new()
      {
          ColumnName = entity.ColumnName,
          ColumnOrdinal = entity.ColumnOrdinal,
          ColumnSize = entity.ColumnSize,
          IsAutoIncrement = entity.IsAutoIncrement,
          IsKey = entity.IsKey,
          IsLong = entity.IsLong,
          IsUnique = entity.IsUnique,
          ClrDataTypeFullName = entity.DataType.FullName ?? "",
          DataTypeName = entity.DataTypeName,
      };

    public static QTableSchemaResponse ToResponse(this QTableSchema entity)
      => new()
      {
          SchemaName = entity.SchemaName,
          TableName = entity.TableName,
          TableOwner = entity.TableOwner,
      };

    public static IReadOnlyCollection<QTableResponse> ToResponse(this IEnumerable<QTable> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<QTableColumnResponse> ToResponse(this IEnumerable<QTableColumn> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<QTableSchemaResponse> ToResponse(this IEnumerable<QTableSchema> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyDictionary<string, QTableColumnResponse> ToResponse(this IDictionary<string, QTableColumn> dict)
        => dict.Values.ToDictionary(s => s.ColumnName, ToResponse);

}
