namespace Mars.Datasource.Dto;

public class QDatabaseStructureResponse
{
    public required string DatabaseName { get; init; }
    public required IReadOnlyCollection<QTableResponse> Tables { get; init; }
}
