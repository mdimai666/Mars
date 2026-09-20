using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Datasource.Abstractions.Mappings;

public class QTableSchema
{
    [Column("schemaname")]
    public string SchemaName { get; set; } = "";
    [Column("tablename")]
    public string TableName { get; set; } = "";
    [Column("tableowner")]
    public string TableOwner { get; set; } = "";

    /// <summary>table | view | matview</summary>
    [Column("kind")]
    public string Kind { get; set; } = QTableKind.Table;

    public bool IsView => Kind != QTableKind.Table;

    /// <summary>Имя для дерева: со схемой, если схема задана.</summary>
    public string DisplayName => string.IsNullOrEmpty(SchemaName) ? TableName : $"{SchemaName}.{TableName}";

    public QTableSchema()
    {

    }
}

public static class QTableKind
{
    public const string Table = "table";
    public const string View = "view";
    public const string MaterializedView = "matview";
}
