namespace Mars.Datasource.Abstractions.Models;

public class QDatabaseStructure
{
    public string DatabaseName { get; set; } = "";
    public List<QTable> Tables { get; set; } = [];
}
