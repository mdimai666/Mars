using AutoFixture;
using Bogus;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Core.Features;
using Mars.Test.Common.Constants;

namespace Mars.Test.Common.FixtureCustomizes;

public static class PostFixtureCustomizeExtension
{
    public static CreatePostQuery CreatePost(this IFixture fixture, string content, string postType = "post", string? slug = null)
    {
        var faker = new Faker("ru");

        return fixture.Build<CreatePostQuery>()
                        .OmitAutoProperties()
                        .With(s => s.Id)
                        .With(s => s.Title, fixture.Create("Title - "))
                        .With(s => s.Content, content)
                        .With(s => s.Status, "publish")
                        .With(s => s.Slug, slug ?? TextTool.TranslateToPostSlug(fixture.Create("slug")))
                        .With(s => s.UserId, UserConstants.TestUserId)
                        .With(s => s.Type, postType)
                        .With(s => s.Tags, [])
                        .With(s => s.LangCode, "")
                        .With(s => s.MetaValues, [])
                        .With(s => s.CategoryIds, [])
                        .Create();
    }
}
