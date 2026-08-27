using System.ComponentModel.DataAnnotations.Schema;

namespace Mars.Datasource.Core;

public class QTableSchema
{
    //[QColumnOrdinal(0)]
    [Column("schemaname")]
    public string SchemaName { get; set; } = "";
    [Column("tablename")]
    public string TableName { get; set; } = "";
    [Column("tableowner")]
    public string TableOwner { get; set; } = "";
    //[Column("tablespace")]
    //public string TableSpace { get; set; }
    //[Column("hasindexes")]
    //public bool HasIndexes { get; set; }
    //[Column("hasrules")]
    //public bool HasRules { get; set; }
    //[Column("hastriggers")]
    //public bool HasTriggers { get; set; }
    //[Column("rowsecurity")]
    //public bool RowSecurity { get; set; }

    public QTableSchema()
    {

    }



}
