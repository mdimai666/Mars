using FluentAssertions;
using Mars.Datasource.Contracts.Ai;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Tests;

/// <summary>
/// Каталог источника → компактный текст схемы для AI-агента (get_source_schema):
/// объекты, поля, параметры операций, фильтр и бюджет размера.
/// </summary>
public class DatasourceSchemaTextTests
{
    [Fact]
    public void Build_SqlCatalog_RendersTablesAndFields()
    {
        var catalog = SqlCatalog();

        var text = DatasourceSchemaText.Build(catalog, "", 10_000);

        text.Should().Contain("kind=sql").And.Contain("driver=psql");
        text.Should().Contain("public.posts (table): *id:uuid, title:text, blog_id:bigint");
        text.Should().Contain("public.users (table): *id:uuid");
        text.Should().Contain("Объектов: 2");
    }

    [Fact]
    public void Build_RestCatalog_RendersOperationsWithParameters_AndMarksWrite()
    {
        var catalog = RestCatalog();

        var text = DatasourceSchemaText.Build(catalog, "", 10_000);

        text.Should().Contain("GET /wp/v2/posts — операция");
        text.Should().Contain("page(query,integer)");
        text.Should().Contain("per_page(query,integer,по умолчанию: 10)");
        text.Should().Contain("POST /wp/v2/posts — операция (запись)");
        text.Should().Contain("title(body,string,обязательный)");
        text.Should().NotContain("GET /wp/v2/posts — операция (запись)");
    }

    [Fact]
    public void Build_FileCatalog_RendersFilesWithTypedColumns()
    {
        var catalog = FileCatalog();

        var text = DatasourceSchemaText.Build(catalog, "", 10_000);

        text.Should().Contain("kind=file");
        text.Should().Contain("people.csv (file): name:text, age:bigint");
    }

    [Fact]
    public void Build_Filter_SelectsMatchingObjectsOnly()
    {
        var catalog = SqlCatalog();

        var text = DatasourceSchemaText.Build(catalog, "users", 10_000);

        text.Should().Contain("public.users");
        text.Should().NotContain("public.posts (");
        text.Should().Contain("Объектов: 1 (из 2, фильтр \"users\")");
    }

    [Fact]
    public void Build_BudgetExceeded_TruncatesWithHint()
    {
        var catalog = SqlCatalog();
        var budget = "Источник".Length + 200; // заведомо меньше двух объектов, но больше первого

        var full = DatasourceSchemaText.Build(catalog, "", 10_000);
        var firstEntryLength = full.IndexOf("public.users", StringComparison.Ordinal);
        firstEntryLength.Should().BeGreaterThan(0);

        var text = DatasourceSchemaText.Build(catalog, "", firstEntryLength);

        text.Should().Contain("public.posts");
        text.Should().NotContain("public.users");
        text.Should().Contain("показаны 1 из 2 объектов").And.Contain("сузи фильтр");
    }

    [Fact]
    public void Build_EnumParameter_RendersChoices()
    {
        var catalog = RestCatalog();
        catalog.Groups[0].Objects[0].Operation!.Parameters.Add(new DatasourceOperationParameter
        {
            Name = "orderby",
            In = DatasourceParameterIn.Query,
            Type = "string",
            Enum = ["date", "title", "id"],
        });

        var text = DatasourceSchemaText.Build(catalog, "", 10_000);

        text.Should().Contain("orderby(query,string,один из: date|title|id)");
    }

    static DatasourceCatalog SqlCatalog() => new()
    {
        SourceName = "Моя база",
        Profile = new DatasourceKindProfile { Kind = DatasourceKind.Sql, Driver = "psql" },
        Groups =
        [
            new DatasourceCatalogGroup
            {
                Name = "public",
                Objects =
                [
                    new DatasourceCatalogObject
                    {
                        Id = "public.posts",
                        Name = "posts",
                        ObjectType = DatasourceObjectType.Table,
                        Fields =
                        [
                            new DatasourceField { Name = "id", DataTypeName = "uuid", IsKey = true },
                            new DatasourceField { Name = "title", DataTypeName = "text" },
                            new DatasourceField { Name = "blog_id", DataTypeName = "bigint" },
                        ],
                    },
                    new DatasourceCatalogObject
                    {
                        Id = "public.users",
                        Name = "users",
                        ObjectType = DatasourceObjectType.Table,
                        Fields = [new DatasourceField { Name = "id", DataTypeName = "uuid", IsKey = true }],
                    },
                ],
            },
        ],
    };

    static DatasourceCatalog RestCatalog() => new()
    {
        SourceName = "WordPress",
        Profile = new DatasourceKindProfile { Kind = DatasourceKind.Rest, DefaultLanguage = DatasourceLanguage.Http },
        Groups =
        [
            new DatasourceCatalogGroup
            {
                Name = "GET",
                Objects =
                [
                    new DatasourceCatalogObject
                    {
                        Id = "GET /wp/v2/posts",
                        Name = "posts",
                        ObjectType = DatasourceObjectType.Operation,
                        Operation = new DatasourceOperation
                        {
                            Method = "GET",
                            Parameters =
                            [
                                new DatasourceOperationParameter { Name = "page", In = DatasourceParameterIn.Query, Type = "integer" },
                                new DatasourceOperationParameter { Name = "per_page", In = DatasourceParameterIn.Query, Type = "integer", Default = "10" },
                            ],
                        },
                    },
                ],
            },
            new DatasourceCatalogGroup
            {
                Name = "POST",
                Objects =
                [
                    new DatasourceCatalogObject
                    {
                        Id = "POST /wp/v2/posts",
                        Name = "posts",
                        ObjectType = DatasourceObjectType.Operation,
                        Operation = new DatasourceOperation
                        {
                            Method = "POST",
                            Parameters =
                            [
                                new DatasourceOperationParameter { Name = "title", In = DatasourceParameterIn.Body, Type = "string", Required = true },
                            ],
                        },
                    },
                ],
            },
        ],
    };

    static DatasourceCatalog FileCatalog() => new()
    {
        SourceName = "Файлы",
        Profile = new DatasourceKindProfile { Kind = DatasourceKind.File, DefaultLanguage = DatasourceLanguage.Linq },
        Groups =
        [
            new DatasourceCatalogGroup
            {
                Name = "Файлы",
                Objects =
                [
                    new DatasourceCatalogObject
                    {
                        Id = "people.csv",
                        Name = "people.csv",
                        ObjectType = DatasourceObjectType.File,
                        Fields =
                        [
                            new DatasourceField { Name = "name", DataTypeName = "text" },
                            new DatasourceField { Name = "age", DataTypeName = "bigint" },
                        ],
                    },
                ],
            },
        ],
    };
}
