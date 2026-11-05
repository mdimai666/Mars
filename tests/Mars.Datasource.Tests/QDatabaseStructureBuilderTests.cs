using FluentAssertions;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Tests;

/// <summary>
/// Сборка структуры из плоских метаданных: дубли строк колонок (грязный join каталога,
/// как в MySQL с общим именем ограничения PRIMARY) не должны ронать Assemble.
/// </summary>
public class QDatabaseStructureBuilderTests
{
    static QColumnMeta Column(string table, string name, int ordinal, bool isKey = false) => new()
    {
        SchemaName = "mars",
        TableName = table,
        ColumnName = name,
        ColumnOrdinal = ordinal,
        DataTypeName = "bigint",
        IsNullable = false,
        IsKey = isKey,
    };

    [Fact]
    public void Assemble_DuplicateColumnMeta_KeepsFirstRow()
    {
        QTableMeta[] tables =
        [
            new() { SchemaName = "mars", TableName = "posts", Kind = QTableKind.Table },
        ];

        // meta_id трижды — так выглядел раздутый LEFT JOIN pk-подзапроса MySQL.
        QColumnMeta[] columns =
        [
            Column("posts", "meta_id", 1, isKey: true),
            Column("posts", "title", 2),
            Column("posts", "meta_id", 1, isKey: true),
            Column("posts", "meta_id", 1, isKey: true),
        ];

        var structure = QDatabaseStructureBuilder.Assemble("mars", tables, columns);

        var posts = structure.Tables.Should().ContainSingle().Subject;
        posts.Columns.Keys.Should().Equal("meta_id", "title");
        posts.Columns["meta_id"].IsKey.Should().BeTrue();
    }

    [Fact]
    public void Assemble_OrdersColumnsByOrdinal()
    {
        QTableMeta[] tables =
        [
            new() { SchemaName = "mars", TableName = "posts", Kind = QTableKind.Table },
        ];

        QColumnMeta[] columns =
        [
            Column("posts", "title", 2),
            Column("posts", "id", 1),
        ];

        var structure = QDatabaseStructureBuilder.Assemble("mars", tables, columns);

        structure.Tables[0].Columns.Keys.Should().Equal("id", "title");
    }

    [Fact]
    public void Assemble_TableWithoutColumns_GetsEmptyDictionary()
    {
        QTableMeta[] tables =
        [
            new() { SchemaName = "mars", TableName = "empty_view", Kind = QTableKind.View },
        ];

        var structure = QDatabaseStructureBuilder.Assemble("mars", tables, []);

        structure.Tables.Should().ContainSingle().Which.Columns.Should().BeEmpty();
    }
}
