#if !NOADMIN
using AppAdmin.Pages.PostsViews;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Repositories;

#endif
using Mars.Shared.Contracts.XActions;

namespace Mars.XActions.ContentRecipes;

[RegisterXActionCommand(CommandId, "Create paginator block template")]
public class CreatePaginatorBlockTemplateAct(IPostRepository postRepository, IRequestContext requestContext) : IAct
{
    public const string CommandId = "Mars.XActions.Content.Templates." + nameof(CreatePaginatorBlockTemplateAct);

    public static XActionCommand XAction { get; } = new XActionCommand()
    {
        Id = CommandId,
        Label = "Создать пример paginator",
#if !NOADMIN
        FrontContextId = [typeof(ManagePostPage).FullName + "-block"],
#endif
        Type = XActionType.HostAction
    };

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var blockKey = "paginator";
        var containerTypeName = "block";
        var postExist = await postRepository.ExistAsync(containerTypeName, blockKey, cancellationToken);

        if (postExist) return XActResult.ToastWarning($"{containerTypeName} with slug '{blockKey}' is already exist. Please rename this.");

        await postRepository.Create(new CreatePostQuery
        {
            Title = blockKey,
            Slug = blockKey,
            Excerpt = null,
            Content = BlockTemplate,
            LangCode = "",
            MetaValues = [],
            Status = null,
            Tags = [blockKey, "generated"],
            Type = containerTypeName,
            UserId = requestContext.User.Id
        }, cancellationToken);

        return XActResult.ToastSuccess($"{containerTypeName} '{blockKey}' is created");
    }

    public const string BlockTemplate = """
        <div class="hstack">
            {{#with paginator}}
                <span class="position-absolute text-nowrap text-secondary">Всего {{Total}} записей</span>
                <nav class="hstack justify-content-center w-100" aria-label="Pagination">
                    <div class="me-3">
                        {{#if prev}}
                            <div class="page-item ">
                                <a class="btn btn-primary rounded-1 px-2 hstack" 
                                    style="height: 36px;"
                                    href="{{@parent.url}}{{@parent.url2}}?{{prev}}">
                                    <i class="bi bi-arrow-left"></i>
                                    {{!-- <span class="ms-3">Назад</span> --}}
                                </a>
                            </div>
                        {{/if}}
                    </div>

                    <ul class="pagination pagination-sm 1justify-content-start m-0" 1style="margin:20px 0">
                    {{!-- {{#if prev}}
                        <li class="page-item ">
                            <a class="page-link" href="{{@parent.url}}{{@parent.url2}}?{{prev}}">&lt;</a>
                        </li>
                    {{/if}} --}}
                    {{#each items}}
                        <li class="page-item {{#eqstr @key @parent.page}}active{{/eqstr}}">
                            <a class="page-link px-3  py-2 border-0 rounded-1" href="{{../../url}}{{../../url2}}?{{.}}">{{@key}}</a>
                        </li>
                    {{/each}}
                    {{!-- {{#if next}}
                        <li class="page-item">
                            <a class="page-link" href="{{@parent.url}}{{@parent.url2}}?{{next}}">&gt;</a>
                        </li>
                    {{/if}} --}}
                    </ul>

                    <div class="ms-3">
                        {{#if next}}
                            <div class="page-item">
                                <a class="btn btn-primary rounded-1 px-2 hstack"  
                                    style="height: 36px;"
                                    href="{{@parent.url}}{{@parent.url2}}?{{next}}">
                                    {{!-- <span class="me-3">Вперед</span> --}}
                                    <i class="bi bi-arrow-right"></i>
                                </a>
                            </div>
                        {{/if}}
                    </div>

                </div>
            {{/with}}

        </div>
        """;
}
