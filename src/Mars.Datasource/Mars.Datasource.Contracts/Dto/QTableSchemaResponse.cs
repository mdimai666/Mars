namespace Mars.Datasource.Contracts.Dto;

public record QTableSchemaResponse
{
    public required string SchemaName { get; init; }
    public required string TableName { get; init; }
    public required string TableOwner { get; init; }

    /// <summary>table | view | matview</summary>
    public required string Kind { get; init; }

    public bool IsView => Kind != "table";
}
