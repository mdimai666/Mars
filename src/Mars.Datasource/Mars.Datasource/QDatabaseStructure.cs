namespace Mars.Datasource.Core;

public class QDatabaseStructure
{
    public string DatabaseName { get; set; } = "";
    public List<QTable> Tables { get; set; } = new();
}
