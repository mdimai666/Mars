namespace Mars.Datasource.Core.Dto;

public class QDatabaseStructureResponse
{
    public required string DatabaseName { get; init; }
    public required IReadOnlyCollection<QTableResponse> Tables { get; init; }
}
