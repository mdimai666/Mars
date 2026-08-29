using Bogus;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.XActions;
using Mars.Identity.Abstractions.Interfaces;

namespace Mars.XActions.ContentRecipes;

public class CreateMockPostsAct(
    IPostService postService,
    IMetaModelTypesLocator metaModelTypesLocator,
    IRequestContext requestContext) : IAct
{
    public const string CommandId = "mars.content.createMockPosts";
    public const string PostTypeArg = "postType";

    /// <summary>
    /// Ключ динамического источника вариантов выбора типа записей.
    /// </summary>
    public const string PostTypesOptionsSource = "postTypes";

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        int count = 10;
        var postTypeName = context.Get(PostTypeArg);
        if (string.IsNullOrWhiteSpace(postTypeName)) postTypeName = "post";

        int postCount = (await postService.ListTable(new() { Type = postTypeName }, cancellationToken)).TotalCount ?? 0;

        var postType = metaModelTypesLocator.GetPostTypeByName(postTypeName);
        var statusSlug = postType.PostStatusList.FirstOrDefault()?.Slug ?? "";

        var faker = new Faker("ru");

        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int next = postCount + i;
            var post = new CreatePostQuery
            {
                Title = faker.Commerce.ProductName(),
                Content = "<p>" + faker.Lorem.Paragraphs(1, 3, "</p><p>") + "</p>",
                Excerpt = "",
                LangCode = "ru",
                MetaValues = [],
                Slug = $"post-mock-{next}",
                Status = statusSlug,
                Tags = ["mock"],
                Type = postTypeName,
                UserId = requestContext.User.Id,
                CategoryIds = [],
            };
            await postService.Create(post, cancellationToken);
        }

        return XActResult.ToastSuccess("mock posts created");
    }
}
