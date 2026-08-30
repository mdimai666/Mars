using FluentAssertions;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;

namespace Mars.WebApiClient.Integration.Tests.Tests.PostTypes;

public sealed class ListMetaValueRelationModelsTests : BaseWebApiClientTests
{
    public ListMetaValueRelationModelsTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task ListMetaValueRelationModels_PostSubType_FiltersByTypeName()
    {
        //Arrange
        var client = GetWebApiClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var photoType = $"photo_{suffix}";
        var articleType = $"article_{suffix}";

        await client.PostType.Create(CreateTypeRequest(photoType));
        await client.PostType.Create(CreateTypeRequest(articleType));

        var photoPost1 = await client.Post.Create(CreatePostRequest(photoType, $"photo-1-{suffix}"));
        var photoPost2 = await client.Post.Create(CreatePostRequest(photoType, $"photo-2-{suffix}"));
        await client.Post.Create(CreatePostRequest(articleType, $"article-1-{suffix}"));

        //Act
        var result = await client.PostType.ListMetaValueRelationModels(new()
        {
            Skip = 0,
            Take = 50,
            ModelName = $"Post.{photoType}",
        });

        //Assert
        result.Items.Select(s => s.Id).Should().BeEquivalentTo([photoPost1.Id, photoPost2.Id]);
    }

    static CreatePostTypeRequest CreateTypeRequest(string typeName)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = typeName,
            TypeName = typeName,
            Tags = [],
            PostStatusList = [],
            EnabledFeatures = [],
            Disabled = false,
            Visibility = PostTypeVisibility.Public,
            MetaFields = [],
        };

    static CreatePostRequest CreatePostRequest(string typeName, string slug)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = slug,
            Type = typeName,
            Slug = slug,
            Tags = [],
            Content = null,
            Status = null,
            Excerpt = null,
            LangCode = "",
            CategoryIds = [],
            MetaValues = [],
        };
}
