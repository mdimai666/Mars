namespace Mars.Datasource.Core.Dto;

public record QTableColumnResponse
{
    public required string ColumnName { get; init; }
    public required int ColumnOrdinal { get; init; }
    public required int? ColumnSize { get; init; }
    public required bool? IsAutoIncrement { get; init; }
    public required bool? IsKey { get; init; }
    public required bool? IsLong { get; init; }
    public required bool? IsUnique { get; init; }
    public required string ClrDataTypeFullName { get; init; }
    public required string DataTypeName { get; init; }
}
