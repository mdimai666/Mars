namespace Mars.Datasource.Core;

public class QTableColumn
{
    public string ColumnName { get; set; } = "";
    public int ColumnOrdinal { get; set; }
    public int? ColumnSize { get; set; }
    public bool? IsAutoIncrement { get; set; }
    public bool? IsKey { get; set; }
    public bool? IsLong { get; set; }
    public bool? IsUnique { get; set; }
    public Type DataType { get; set; } = default!;
    public string DataTypeName { get; set; } = "";

    public QTableColumn()
    {

    }

}
