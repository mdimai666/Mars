using AutoFixture;
using FluentAssertions;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.Search;
using Mars.Core.Exceptions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.Searches;

/// <summary>
/// GET /api/Search/Query — глобальный поиск для палитры команд
/// (эндпоинт вынесен из ViewModelController.GlobalSearch).
/// </summary>
public class QuerySearchTests : BaseWebApiClientTests
{
    public QuerySearchTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task Query_ValidText_FindsCreatedPost()
    {
        //Arrange
        var client = GetWebApiClient();
        var token = "zzsrch" + Guid.NewGuid().ToString("N")[..8];
        var request = _fixture.Create<CreatePostRequest>() with { Title = $"Unique {token} Title" };
        var post = await client.Post.Create(request);

        //Act
        var results = await client.Search.Query(token);

        //Assert
        results.Should().Contain(s => s.Type == FoundElementType.Record
                                      && s.Url == $"/dev/EditPost/post/{post.Id}");
    }

    [IntegrationFact]
    public async Task Query_ShortText_ReturnsEmpty()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var oneChar = await client.Search.Query("a");
        var empty = await client.Search.Query("  ");

        //Assert
        oneChar.Should().BeEmpty();
        empty.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Query_MaxCountTooLarge_ThrowsValidation()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var action = () => client.Search.Query("text", maxCount: 31);

        //Assert
        (await action.Should().ThrowAsync<MarsValidationException>())
            .And.Errors.Should().ContainKey("maxCount");
    }

    [IntegrationFact]
    public async Task Query_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(isAnonymous: true);

        //Act
        var action = () => client.Search.Query("text");

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
