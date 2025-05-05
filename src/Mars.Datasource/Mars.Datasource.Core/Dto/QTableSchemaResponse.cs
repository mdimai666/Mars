namespace Mars.Datasource.Core.Dto;

public record QTableSchemaResponse
{
    public required string SchemaName { get; init; }
    public required string TableName { get; init; }
    public required string TableOwner { get; init; }

}
