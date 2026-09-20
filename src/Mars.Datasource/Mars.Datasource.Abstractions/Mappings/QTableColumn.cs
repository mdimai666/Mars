namespace Mars.Datasource.Abstractions.Mappings;

public class QTableColumn
{
    public string ColumnName { get; set; } = "";
    public int ColumnOrdinal { get; set; }

    /// <summary>Размер из каталога: у MySQL longtext/JSON он больше int (4294967295).</summary>
    public long? ColumnSize { get; set; }

    public bool? IsAutoIncrement { get; set; }
    public bool? IsKey { get; set; }
    public bool? IsLong { get; set; }
    public bool? IsUnique { get; set; }
    public bool IsNullable { get; set; } = true;

    /// <summary>json/jsonb-колонка: значение показываем деревом.</summary>
    public bool IsJson { get; set; }

    public Type DataType { get; set; } = typeof(object);
    public string DataTypeName { get; set; } = "";

}
