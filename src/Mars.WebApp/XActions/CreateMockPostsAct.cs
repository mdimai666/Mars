#if !NOADMIN
using AppAdmin.Pages.PostsViews;
#endif
using Bogus;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;

namespace Mars.XActions;

public class CreateMockPostsAct(
    IPostService postService,
    IMetaModelTypesLocator metaModelTypesLocator,
    IRequestContext requestContext) : IAct
{
    public static XActionCommand XAction { get; } = new XActionCommand()
    {
        Id = typeof(CreateMockPostsAct).FullName!,
        Label = "Create mock posts",
#if !NOADMIN
        FrontContextId = [typeof(ManagePostPage).FullName + "-post"],
#endif
        Type = XActionType.HostAction
    };

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        int count = 10;
        int postCount = (await postService.ListTable(new() { Type = "post" }, cancellationToken)).TotalCount ?? 0;

        var postType = metaModelTypesLocator.GetPostTypeByName("post");
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
                Type = "post",
                UserId = requestContext.User.Id,
            };
            await postService.Create(post, cancellationToken);
        }

        return XActResult.ToastSuccess("mock posts created");
    }
}
