using AutoFixture;
using FluentAssertions;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Contracts.PostTypes;
using Mars.Test.Common.FixtureCustomizes;

namespace Mars.WebApiClient.Integration.Tests.Tests.Posts;

/// <summary>Контракт <see cref="Mars.WebApiClient.Interfaces.IPostServiceClient.Single"/> — единственная запись single-типа.</summary>
public sealed class SinglePostTests : BaseWebApiClientTests
{
    public SinglePostTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task SinglePost_SingleType_ReturnsTheOnlyPost()
    {
        //Arrange
        _ = nameof(MarsWebApiClient.Post.Single);
        var client = GetWebApiClient();
        var request = _fixture.Create<CreatePostTypeRequest>() with
        {
            MetaFields = [],
            PostStatusList = [],
            EnabledFeatures = [PostTypeConstants.Features.Single],
            ImageFieldKey = null,
        };
        var postType = await client.PostType.Create(request);

        //Act
        var single = await client.Post.Single(postType.TypeName);

        //Assert
        single.Should().NotBeNull();
        single.Type.Should().Be(postType.TypeName);
        single.Title.Should().Be(postType.Title);
    }
}
