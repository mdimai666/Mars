namespace Mars.Datasource.Abstractions.Mappings;

public class QDatabaseStructure
{
    public string DatabaseName { get; set; } = "";
    public List<QTable> Tables { get; set; } = [];
}
