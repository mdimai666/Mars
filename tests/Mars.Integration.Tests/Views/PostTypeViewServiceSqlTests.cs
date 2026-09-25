using FluentAssertions;
using Mars.Cms.Host.Services;

namespace Mars.Integration.Tests.Views;

/// <seealso cref="PostTypeViewService"/>
public class PostTypeViewServiceSqlTests
{
    [Fact]
    public void GetViewName_SimpleTypeName_MustPrefixAndLowercase()
    {
        PostTypeViewService.GetViewName("Blog-Post").Should().Be("mt_view_blog_post");
    }

    [Theory]
    [InlineData("post")]
    [InlineData("Blog-Post")]
    [InlineData("post\"; DROP TABLE posts; --")]
    [InlineData("x' UNION SELECT password FROM users --")]
    public void GetViewName_AnyInput_MustProduceSafeIdentifier(string typeName)
    {
        //Act
        var viewName = PostTypeViewService.GetViewName(typeName);

        //Assert — из имени представления не должны выживать кавычки, апострофы и разделители операторов
        viewName.Should().StartWith("mt_view_");
        viewName.Should().MatchRegex("^[a-z0-9_]+$");
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("views_count")]
    [InlineData("relation_fieldId")]
    [InlineData("subtitleVariantId")]
    public void QuoteIdentifier_SafeName_MustWrapInDoubleQuotes(string identifier)
    {
        PostTypeViewService.QuoteIdentifier(identifier).Should().Be($"\"{identifier}\"");
    }

    [Theory]
    [InlineData("a\"b")]
    [InlineData("a'b")]
    [InlineData("a;b")]
    [InlineData("a b")]
    [InlineData("")]
    public void QuoteIdentifier_InvalidName_MustThrow(string identifier)
    {
        //Act
        var act = () => PostTypeViewService.QuoteIdentifier(identifier);

        //Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("post", "'post'")]
    [InlineData("O'Brien", "'O''Brien'")]
    [InlineData("x'; DROP TABLE posts; --", "'x''; DROP TABLE posts; --'")]
    public void QuoteLiteral_MustEscapeSingleQuotes(string value, string expected)
    {
        PostTypeViewService.QuoteLiteral(value).Should().Be(expected);
    }
}
