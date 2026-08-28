using Mars.SiteEngine.Abstractions.Services;
using Mars.Services;
using Mars.Contracts.XActions;
using Mars.SiteEngine.Services;

namespace Mars.XActions.ContentRecipes;

/// <summary>
/// Создаёт шаблон представления списка записей в специальном фронте админки
/// (data/admin/front) по пути postTypes/&lt;typeName&gt;/listView.hbs.
/// </summary>
public class CreatePostTypePresentationTemplateAct(IFrontFilesService frontFilesService) : IAct
{
    public const string CommandId = "mars.content.templates.createPresentation";
    public const string PostTypeNameArg = "postTypeName";

    public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var postTypeName = context.Get(PostTypeNameArg);
        if (string.IsNullOrWhiteSpace(postTypeName))
            return Task.FromResult(XActResult.ToastError($"required '{PostTypeNameArg}' argument"));

        var relPath = ListViewRelPath(postTypeName);

        var fullPath = frontFilesService.ResolveSafePath(FrontManager.AdminFrontSlug, relPath);
        if (File.Exists(fullPath))
            return Task.FromResult(XActResult.ToastWarning($"file '{relPath}' already exists"));

        frontFilesService.SaveFile(FrontManager.AdminFrontSlug, relPath, GeneratePageTemplate(postTypeName));

        return Task.FromResult(XActResult.ToastSuccess($"created '{relPath}' in admin front"));
    }

    /// <summary>
    /// Правило именования шаблона списка: postTypes/&lt;typeName&gt;/listView.hbs
    /// </summary>
    public static string ListViewRelPath(string postTypeName)
        => $"postTypes/{postTypeName}/listView.hbs";

    string GeneratePageTemplate(string postTypeName) => $$$$"""
        @page "/postTypes/{{{{postTypeName}}}}/listView"

        {{#context}}
        page == int.Parse(_req.Query["page"]??"1")
        pageSize == 20
        table=ef.{{{{postTypeName}}}}.Table(page, pageSize)
        {{/context}}

        <div class="p-3">
        <div class="hstack mb-3">
        <div class="ms-auto">
        <a href="/dev/EditPost/{{{{postTypeName}}}}" class="btn btn-primary">Create {{{{postTypeName}}}}</a>
        </div>
        </div>

        <div class="row row-cols-lg-3 g-3">
        {{#each table.Items}}
        <div class="col">
        <div class="card d-card-glow">
        <div class="card-body">
        <h5 class="card-title">{{title}}</h5>
        <div class="vstack gap-1">
        <div class="text-secondary">{{slug}}</div>
        </div>
        <a href="/dev/EditPost/{{{{postTypeName}}}}/{{id}}" class="stretched-link"></a>
        </div>
        </div>
        </div>
        {{/each}}
        </div>
        </div>
        """;
}
