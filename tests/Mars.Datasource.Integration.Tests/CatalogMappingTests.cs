using FluentAssertions;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Integration.Tests;

/// <summary>
/// Структура базы → каталог источника: одно дерево для всех типов источников,
/// поэтому sql-специфичные поля (схема, kind объекта, PK) обязаны доезжать до фронта.
/// </summary>
public class CatalogMappingTests
{
    static QDatabaseStructure Structure() => new()
    {
        DatabaseName = "mars",
        Tables =
        [
            new QTable
            {
                TableName = "todo",
                TableSchema = new QTableSchema { SchemaName = "public", TableName = "todo", Kind = QTableKind.Table },
                Columns = new Dictionary<string, QTableColumn>
                {
                    ["title"] = new QTableColumn { ColumnName = "title", ColumnOrdinal = 2, DataTypeName = "character varying", DataType = typeof(string), IsNullable = true },
                    ["id"] = new QTableColumn { ColumnName = "id", ColumnOrdinal = 1, DataTypeName = "uuid", DataType = typeof(Guid), IsNullable = false, IsKey = true },
                },
            },
            new QTable
            {
                TableName = "todo_view",
                TableSchema = new QTableSchema { SchemaName = "public", TableName = "todo_view", Kind = QTableKind.View },
                Columns = new Dictionary<string, QTableColumn>(),
            },
            new QTable
            {
                TableName = "shop_orders",
                TableSchema = new QTableSchema { SchemaName = "shop", TableName = "shop_orders", Kind = QTableKind.Table },
                Columns = new Dictionary<string, QTableColumn>
                {
                    ["data"] = new QTableColumn { ColumnName = "data", ColumnOrdinal = 1, DataTypeName = "jsonb", DataType = typeof(string), IsJson = true, ColumnSize = 4294967295 },
                },
            },
        ],
    };

    [Fact]
    public void FromStructure_GroupsBySchemaInOrder()
    {
        var catalog = CatalogMapping.FromStructure(Structure(), new DatasourceConfig());

        catalog.SourceName.Should().Be("mars");
        catalog.Kind.Should().Be(DatasourceKind.Sql);
        catalog.Groups.Select(g => g.Name).Should().Equal("public", "shop");
        catalog.Groups[0].Objects.Select(o => o.Name).Should().Equal("todo", "todo_view");
    }

    [Fact]
    public void FromStructure_ObjectIdIsSchemaQualified()
    {
        var catalog = CatalogMapping.FromStructure(Structure(), new DatasourceConfig());

        catalog.Groups[0].Objects[0].Id.Should().Be("public.todo");
        catalog.Groups[1].Objects[0].Id.Should().Be("shop.shop_orders");
    }

    [Fact]
    public void FromStructure_EmptySchemaName_GivesBareId()
    {
        var structure = new QDatabaseStructure
        {
            DatabaseName = "shop",
            Tables =
            [
                new QTable
                {
                    TableName = "orders",
                    TableSchema = new QTableSchema { SchemaName = "", TableName = "orders" },
                    Columns = new Dictionary<string, QTableColumn>(),
                },
            ],
        };

        var catalog = CatalogMapping.FromStructure(structure, new DatasourceConfig());

        catalog.Groups.Single().Name.Should().BeEmpty();
        catalog.Groups.Single().Objects.Single().Id.Should().Be("orders");
    }

    [Fact]
    public void FromStructure_KeepsObjectKindAndQueryLanguage()
    {
        var catalog = CatalogMapping.FromStructure(Structure(), new DatasourceConfig());

        catalog.Groups[0].Objects[0].ObjectType.Should().Be(DatasourceObjectType.Table);
        catalog.Groups[0].Objects[1].ObjectType.Should().Be(DatasourceObjectType.View);
        catalog.Groups[0].Objects.Should().OnlyContain(o => o.DefaultLanguage == DatasourceLanguage.Sql);
    }

    [Fact]
    public void FromStructure_ColumnsOrderedByOrdinalWithCatalogFlags()
    {
        var catalog = CatalogMapping.FromStructure(Structure(), new DatasourceConfig());

        var todo = catalog.Groups[0].Objects[0];
        todo.Columns.Select(c => c.Name).Should().Equal("id", "title");

        var id = todo.Columns[0];
        id.IsKey.Should().BeTrue();
        id.IsNullable.Should().BeFalse();
        id.DataTypeName.Should().Be("uuid");
        id.ClrTypeName.Should().Be(typeof(Guid).FullName);

        var json = catalog.Groups[1].Objects[0].Columns.Single();
        json.IsJson.Should().BeTrue();
        // Размер longtext/JSON в MySQL не влезает в int — в каталоге он обязан остаться long.
        json.Size.Should().Be(4294967295L);
    }

    [Fact]
    public void FromStructure_SqlCapabilities()
    {
        var catalog = CatalogMapping.FromStructure(Structure(), new DatasourceConfig());

        catalog.Capabilities.CanQuery.Should().BeTrue();
        catalog.Capabilities.CanBrowse.Should().BeTrue();
        catalog.Capabilities.CanWrite.Should().BeTrue();
        catalog.Capabilities.CanManageViews.Should().BeTrue();
    }

    [Fact]
    public void FromStructure_ConfigWithoutKind_FallsBackToSql()
    {
        var catalog = CatalogMapping.FromStructure(Structure(), new DatasourceConfig { Kind = " " });

        catalog.Kind.Should().Be(DatasourceKind.Sql);
    }

    [Fact]
    public void ObjectId_SchemaAndName()
    {
        CatalogMapping.ObjectId("public", "todo").Should().Be("public.todo");
        CatalogMapping.ObjectId(null, "todo").Should().Be("todo");
        CatalogMapping.ObjectId("", "todo").Should().Be("todo");
    }
}
