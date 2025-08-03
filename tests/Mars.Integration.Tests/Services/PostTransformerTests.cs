using AutoFixture;
using EditorJsBlazored.Blocks;
using EditorJsBlazored.Core;
using FluentAssertions;
using Mars.Host.Services;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.Extensions.DependencyInjection;
using static Mars.Shared.Contracts.PostTypes.PostTypeConstants;

namespace Mars.Integration.Tests.Services;

public class PostTransformerTests : ApplicationTests
{
    private readonly IPostTransformer _postTransformer;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public PostTransformerTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _postTransformer = appFixture.ServiceProvider.GetRequiredService<IPostTransformer>();
        _metaModelTypesLocator = appFixture.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>();
    }

    [IntegrationFact]
    public async Task Transform_BlockEditorJsonContentRender_ShouldReturnHtmlContent()
    {
        //Arrange
        _ = nameof(PostTransformer.Transform);
        var postType = _metaModelTypesLocator.GetPostTypeByName("post")!;
        if (postType.PostContentSettings.PostContentType != DefaultPostContentTypes.BlockEditor)
            throw new NotSupportedException($"PostType must be '{DefaultPostContentTypes.BlockEditor}'. Retrived '{postType.TypeName}'.");

        var content = new EditorJsContent()
        {
            Blocks = [
                new() { Type = "header", Data = new BlockHeader { Text = "Header 1", Level = 1 } },
                new() { Type = "paragraph", Data = new BlockParagraph { Text = "Hello World" } },
            ]
        };
        var post = _fixture.Create<PostDetail>() with
        {
            Content = content.ToJson(),
            Type = "post"
        };

        //Act
        var renderedContent = (await _postTransformer.Transform(post, default)).Content;

        //Assert
        renderedContent.Should().Contain((content.Blocks[0].Data as IEditorJsBlock).GetHtml());
        renderedContent.Should().MatchRegex("<p(.*?)>Hello World</p>");
    }
}
