using Mars.AiChat.Host.Services;
using Mars.AiChat.Host.Tools;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Инструменты открытой страницы админки (мост page bridge), per-run экземпляр с chatId.
/// </summary>
public class PageToolset : IAiToolset
{
    private readonly AiChatPageBridge _pageBridge;

    public PageToolset(AiChatPageBridge pageBridge)
    {
        _pageBridge = pageBridge;
    }

    public string Name => "open-page";

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx)
    {
        var pageTools = new MarsOpenPageTools(_pageBridge, ctx.ChatId);
        return
        [
            AIFunctionFactory.Create(pageTools.GetOpenPageInfo),
            AIFunctionFactory.Create(pageTools.GetOpenPageFields),
            AIFunctionFactory.Create(pageTools.SetOpenPageField),
            AIFunctionFactory.Create(pageTools.SaveOpenPage),
        ];
    }
}
