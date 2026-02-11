#if !NOADMIN
using AppAdmin.Pages.PostTypeViews;
using Mars.Host.Managers;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Repositories;

#endif
using Mars.Shared.Contracts.XActions;

namespace Mars.XActions.ContentRecipes;

[RegisterXActionCommand(CommandId, "Create PostType presentation template")]
public class CreatePostTypePresentationTemplateAct(IPostRepository postRepository, IRequestContext requestContext) : IAct
{
    public const string CommandId = "Mars.XActions.Content.Templates." + nameof(CreatePostTypePresentationTemplateAct);

    public static XActionCommand XAction { get; } = new XActionCommand()
    {
        Id = CommandId,
        Label = "Создать шаблон представления для типа записи",
#if !NOADMIN
        FrontContextId = [typeof(EditPostTypePresentationPage).FullName!],
#endif
        Type = XActionType.HostAction
    };

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        if (context.args.Length != 1)
            return XActResult.ToastError("required 1 (postTypeName) argument");
        //TODO: нет формы, которая бы запрашивала аргумент
        //TODO: если есть action требующий аргумент то его нельзя будет вызвать.

        _ = nameof(EditPostTypePresentationPage);// сравнивать с этим

        var postTypeName = context.args[0];
        var postSlug = $"admin_list_{postTypeName}_page";
        var containerTypeName = "template";

        var postExist = await postRepository.ExistAsync(containerTypeName, postSlug, cancellationToken);
        if (postExist) return XActResult.ToastWarning($"{containerTypeName} with slug '{postSlug}' is already exist. Please rename this.");

        var needCreatePaginator = true;
        if (needCreatePaginator)
        {
            var paginatorAct = new CreatePaginatorBlockTemplateAct(postRepository, requestContext);
            await paginatorAct.Execute(new ActContext(), cancellationToken);
        }

        await postRepository.Create(new CreatePostQuery
        {
            Title = postSlug.Replace("_", " "),
            Slug = postSlug,
            Excerpt = null,
            Content = GeneratePageTemplate(postTypeName),
            LangCode = "",
            MetaValues = [],
            Status = null,
            Tags = [postTypeName, "admin", "page_tempalte", "generated"],
            Type = containerTypeName,
            UserId = requestContext.User.Id,
            CategoryIds = [],
        }, cancellationToken);

        return XActResult.ToastSuccess($"{containerTypeName} '{postSlug}' is created");
    }

    private string GeneratePageTemplate(string postTypeName) => $$$$"""
        {{#context}}
        page == int.Parse(_req.Query["page"]??"1")
        pageSize == 20
        table=ef.{{{{postTypeName}}}}.Table(page, pageSize)
        {{/context}}

        <h1>AdminPageTemplate: {{{{postTypeName}}}}</h1>

        {{!-- {{{#help}}} --}}
        {{!-- <pre>{{#tojson table }}</div> --}}

        <div class="hstack mb-3">
            <div class="ms-auto">
                <a href="EditPost/{{{{postTypeName}}}}" class="btn btn-primary">Create {{{{postTypeName}}}}</a>
            </div>
        </div>

        {{#if 1}}
        <div class="row row-cols-lg-3 g-3">
            {{#each table.Items}}
            <div class="col">
                <div class="card d-card-glow">
                    <div class="card-body">
                        <h5 class="card-title">{{title}}</h5>
                        <div class="vstack gap-1">
                            <div class="text-secondary">{{slug}}</div>
                            <div class="text-">@{{Author.displayName}}</div>
                        </div>
                        <a href="EditPost/post/{{id}}" class="stretched-link"></a>
                    </div>
                </div>
            </div>
            {{/each}}
        </div>
        <div class="my-3">
            {{#if table.HasMoreData}}
                {{>paginator paginator=table.paginator url='Post/{{{{postTypeName}}}}'}}
            {{/if}}
        </div>
        {{/if}}
        """;

}
