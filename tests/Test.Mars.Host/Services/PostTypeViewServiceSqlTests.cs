using FluentAssertions;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Shared.Contracts.MetaFields;

namespace Test.Mars.Host.Services;

public class PostTypeViewServiceSqlTests
{
    static MetaFieldDto Field(MetaFieldType type, string key)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = key,
            Key = key,
            Type = type,
            MaxValue = null,
            MinValue = null,
            Description = "",
            IsNullable = true,
            Default = null,
            Options = null,
            Order = 0,
            Tags = [],
            Hidden = false,
            Disabled = false,
            Variants = null,
            ModelName = null,
        };

    static PostTypeDetail Detail(string typeName, params MetaFieldDto[] fields)
        => new()
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Title = "Test type",
            TypeName = typeName,
            Tags = [],
            EnabledFeatures = [],
            Disabled = false,
            ModifiedAt = null,
            PostStatusList = [],
            PostContentSettings = new PostContentSettingsDto { PostContentType = "PlainText", CodeLang = null },
            MetaFields = fields,
            Presentation = PostTypePresentation.Default(),
        };

    [Theory]
    [InlineData("article", "mt_view_article")]
    [InlineData("My-Type.2", "mt_view_my_type_2")]
    [InlineData("post", "mt_view_post")]
    public void GetViewName_SanitizesTypeName(string typeName, string expected)
    {
        PostTypeViewService.GetViewName(typeName).Should().Be(expected);
    }

    [Fact]
    public void BuildColumns_MapsFieldsToProperties()
    {
        var fields = new[]
        {
            Field(MetaFieldType.String, "subtitle"),
            Field(MetaFieldType.Relation, "author"),
            Field(MetaFieldType.Select, "state"),
            Field(MetaFieldType.SelectMany, "tags_many"),
            Field(MetaFieldType.Query, "computed"),
        };

        var columns = PostTypeViewService.BuildColumns(Detail("article", fields));
        var properties = columns.Select(c => c.Property).ToList();

        properties.Should().Contain(["Id", "Slug", "Title", "CreatedAt", "ModifiedAt", "StatusId", "UserId"]);
        properties.Should().Contain("subtitle");
        properties.Should().Contain("authorId");
        properties.Should().Contain("stateVariantId");
        // массивные и вычислимые поля в плоское представление не попадают
        properties.Should().NotContain("tags_many");
        properties.Should().NotContain("computed");
    }

    [Fact]
    public void BuildViewSql_ContainsViewDefinitionAndValueSubqueries()
    {
        var stringField = Field(MetaFieldType.String, "subtitle");
        var sql = PostTypeViewService.BuildViewSql(Detail("article", stringField), "mt_view_article");

        sql.Should().Contain("CREATE OR REPLACE VIEW \"mt_view_article\"");
        sql.Should().Contain("\"pt\".\"type_name\" = 'article'");
        sql.Should().Contain($"\"mv\".\"meta_field_id\" = '{stringField.Id}'");
        sql.Should().Contain("AS \"subtitle\"");
    }

    [Fact]
    public void BuildViewSql_EscapesQuotesInTypeName()
    {
        var sql = PostTypeViewService.BuildViewSql(Detail("arti'cle"), "mt_view_arti_cle");

        sql.Should().Contain("'arti''cle'");
    }
}
