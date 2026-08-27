namespace Mars.Datasource.Core;

public class QTable
{
    public string TableName { get; set; } = "";
    public QTableSchema TableSchema { get; set; } = default!;
    public Dictionary<string, QTableColumn> Columns { get; set; } = default!;
}
