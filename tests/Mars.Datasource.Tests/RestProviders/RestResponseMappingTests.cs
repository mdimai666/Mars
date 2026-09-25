using System.Net;
using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Providers.Rest;

namespace Mars.Datasource.Tests.RestProviders;

/// <summary>
/// Ответ rest-источника в общий результат: массив объектов становится таблицей,
/// документ уходит в JSON, WordPress сообщает общее число записей заголовком.
/// </summary>
public class RestResponseMappingTests
{
    static RestResponse Response(string body, HttpStatusCode status = HttpStatusCode.OK,
        IReadOnlyDictionary<string, string>? headers = null)
        => new()
        {
            Method = "GET",
            Url = "https://example.org/wp-json/wp/v2/posts",
            StatusCode = (int)status,
            ReasonPhrase = status.ToString(),
            IsSuccess = (int)status is >= 200 and < 300,
            Body = body,
            Headers = headers ?? new Dictionary<string, string>(),
        };

    static QueryResultDto Map(string body, HttpStatusCode status = HttpStatusCode.OK, int maxRows = 0,
        IReadOnlyDictionary<string, string>? headers = null)
        => RestResponseMapping.Map(Response(body, status, headers), "GET /posts", 12, maxRows);

    [Fact]
    public void Map_ArrayOfObjectsBecomesTable()
    {
        var result = Map("""[{"id":1,"title":"a"},{"id":2,"title":"b"}]""");

        result.Ok.Should().BeTrue();
        result.Fields.Select(column => column.Name).Should().Equal("id", "title");
        result.Rows.Should().HaveCount(2);
        result.Rows[1].Should().Equal("2", "b");
        result.Kind.Should().Be(DatasourceKind.Rest);
        result.Command.Should().Be("GET /posts");
        result.ElapsedMs.Should().Be(12);
        result.Json.Should().BeNull();
    }

    [Fact]
    public void Map_InfersColumnTypes()
    {
        var result = Map("""
            [{"id":1,"rating":4.5,"published":true,"date":"2024-03-01T10:20:30","slug":"a"}]
            """);

        result.Fields.Select(column => column.DataTypeName).Should()
            .Equal("bigint", "double precision", "boolean", "timestamp", "text");
    }

    [Fact]
    public void Map_NestedValueIsJsonColumn()
    {
        var result = Map("""[{"id":1,"title":{"rendered":"Привет"}}]""");

        var title = result.Fields.Single(column => column.Name == "title");
        title.IsJson.Should().BeTrue();
        title.DataTypeName.Should().Be("jsonb");
        result.Rows[0][1].Should().Be("""{"rendered":"Привет"}""");
    }

    [Fact]
    public void Map_MissingValueIsNull()
    {
        var result = Map("""[{"id":1,"x":null},{"id":2}]""");

        result.Fields.Select(column => column.Name).Should().Equal("id", "x");
        result.Rows[0][1].Should().BeNull();
        result.Rows[1][1].Should().BeNull();
    }

    [Fact]
    public void Map_ArrayOfValuesBecomesSingleColumn()
    {
        var result = Map("""[1,2,3]""");

        result.Fields.Select(column => column.Name).Should().Equal("value");
        result.Rows.Select(row => row[0]).Should().Equal("1", "2", "3");
    }

    [Fact]
    public void Map_ObjectBecomesSingleRowAndKeepsDocument()
    {
        const string body = """{"id":1,"title":{"rendered":"a"}}""";

        var result = Map(body);

        result.Rows.Should().ContainSingle();
        result.Fields.Select(column => column.Name).Should().Equal("id", "title");
        result.Json.Should().Be(body);
    }

    [Fact]
    public void Map_NonJsonBodyBecomesResponseCell()
    {
        var result = Map("OK");

        result.Ok.Should().BeTrue();
        result.Fields.Select(column => column.Name).Should().Equal("response");
        result.Rows[0][0].Should().Be("OK");
        result.Json.Should().Be("OK");
    }

    [Fact]
    public void Map_ErrorIsNotOkAndCarriesBody()
    {
        var result = Map("""{"code":"rest_post_invalid_id","message":"Неверный ID записи."}""", HttpStatusCode.NotFound);

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("404").And.Contain("https://example.org/wp-json/wp/v2/posts");
        result.Json.Should().Contain("rest_post_invalid_id");
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public void Map_TotalComesFromWordPressHeader()
    {
        var result = Map("""[{"id":1}]""", headers: new Dictionary<string, string>
        {
            ["X-WP-Total"] = "123",
            ["X-WP-TotalPages"] = "13",
        });

        result.Total.Should().Be(123);
    }

    [Fact]
    public void Map_MaxRowsTruncates()
    {
        var result = Map("""[{"id":1},{"id":2},{"id":3}]""", maxRows: 2);

        result.Rows.Should().HaveCount(2);
        result.Truncated.Should().BeTrue();
    }

    [Fact]
    public void Map_EmptyBodyIsSuccessWithoutRows()
    {
        var result = Map("", HttpStatusCode.NoContent);

        result.Ok.Should().BeTrue();
        result.Rows.Should().BeEmpty();
        result.Message.Should().Contain("пустой ответ");
    }

    [Fact]
    public void Map_OversizedTextIsCut()
    {
        var body = new string('x', RestResponseMapping.MaxJsonChars + 10);

        var result = Map(body);

        result.Json!.Length.Should().BeLessThanOrEqualTo(RestResponseMapping.MaxJsonChars + 1);
        result.Truncated.Should().BeTrue();
    }

    [Fact]
    public void Map_InvalidJsonIsShownAsText()
    {
        var result = Map("""{"id":1,""");

        result.Ok.Should().BeTrue();
        result.Fields.Select(column => column.Name).Should().Equal("response");
    }
}
