namespace Mars.Datasource.Contracts.Dto;

public record QTableColumnResponse
{
    public required string ColumnName { get; init; }
    public required int ColumnOrdinal { get; init; }

    /// <summary>Размер из каталога: у MySQL longtext/JSON он больше int (4294967295).</summary>
    public required long? ColumnSize { get; init; }
    public required bool? IsAutoIncrement { get; init; }
    public required bool? IsKey { get; init; }
    public required bool? IsLong { get; init; }
    public required bool? IsUnique { get; init; }
    public required bool IsNullable { get; init; }
    public required bool IsJson { get; init; }
    public required string ClrDataTypeFullName { get; init; }
    public required string DataTypeName { get; init; }
}
