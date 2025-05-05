namespace Mars.Datasource.Core.Dto;

public record QTableResponse
{
    public required string TableName { get; init; }
    public required QTableSchemaResponse TableSchema { get; init; }
    public required IReadOnlyDictionary<string, QTableColumnResponse> Columns { get; init; }
}
