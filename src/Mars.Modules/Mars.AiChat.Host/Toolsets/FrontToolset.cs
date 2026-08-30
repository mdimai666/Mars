using Mars.AiChat.Host.Tools;
using Mars.SiteEngine.Abstractions.Services;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Файлы фронта; включён, когда пользователь открыл редактор фронта
/// (slug парсится из pageContext). Правила работы — в скилле mars-front-editor.
/// </summary>
public class FrontToolset : IAiToolset
{
    private readonly IFrontFilesService _frontFilesService;

    public FrontToolset(IFrontFilesService frontFilesService)
    {
        _frontFilesService = frontFilesService;
    }

    public string Name => "front-files";

    public bool IsEnabled(AiToolsetContext ctx) => ctx.FrontEditorSlug is not null;

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx)
    {
        var frontTools = new MarsFrontFilesTools(_frontFilesService, ctx.FrontEditorSlug!);
        return
        [
            AIFunctionFactory.Create(frontTools.ListFrontFiles),
            AIFunctionFactory.Create(frontTools.ReadFrontFile),
            AIFunctionFactory.Create(frontTools.WriteFrontFile),
            AIFunctionFactory.Create(frontTools.CreateFrontFile),
            AIFunctionFactory.Create(frontTools.RenameFrontFile),
            AIFunctionFactory.Create(frontTools.DeleteFrontFile),
        ];
    }
}
