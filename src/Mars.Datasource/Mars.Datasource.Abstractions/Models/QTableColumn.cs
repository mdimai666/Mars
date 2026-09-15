namespace Mars.Datasource.Abstractions.Models;

public class QTableColumn
{
    public string ColumnName { get; set; } = "";
    public int ColumnOrdinal { get; set; }
    public int? ColumnSize { get; set; }
    public bool? IsAutoIncrement { get; set; }
    public bool? IsKey { get; set; }
    public bool? IsLong { get; set; }
    public bool? IsUnique { get; set; }
    public bool IsNullable { get; set; } = true;
    public Type DataType { get; set; } = typeof(object);
    public string DataTypeName { get; set; } = "";

}
