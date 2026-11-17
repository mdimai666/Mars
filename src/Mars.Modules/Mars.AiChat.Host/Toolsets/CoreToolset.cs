using Mars.AiChat.Host.Hubs;
using Mars.AiChat.Host.Tools;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Ядро: настройки сайта, любые опции, информация о системе, HTTP-запросы, ask_user,
/// обновление InitialSiteData в открытой админке.
/// </summary>
public class CoreToolset : IAiToolset
{
    private readonly MarsSiteTools _siteTools;
    private readonly MarsOptionsTools _optionsTools;
    private readonly MarsSystemTools _systemTools;
    private readonly MarsHttpTools _httpTools;
    private readonly IHubContext<AiChatHub> _aiChatHub;

    public CoreToolset(
        MarsSiteTools siteTools,
        MarsOptionsTools optionsTools,
        MarsSystemTools systemTools,
        MarsHttpTools httpTools,
        IHubContext<AiChatHub> aiChatHub)
    {
        _siteTools = siteTools;
        _optionsTools = optionsTools;
        _systemTools = systemTools;
        _httpTools = httpTools;
        _aiChatHub = aiChatHub;
    }

    public string Name => "core";

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx)
    {
        var siteDataTools = new MarsSiteDataTools(_aiChatHub, ctx.ChatId);
        return
        [
            AIFunctionFactory.Create(_siteTools.GetSiteSettings),
            AIFunctionFactory.Create(_siteTools.UpdateSiteSettings),
            AIFunctionFactory.Create(_optionsTools.ListSiteOptions),
            AIFunctionFactory.Create(_optionsTools.GetSiteOption),
            AIFunctionFactory.Create(_optionsTools.UpdateSiteOption),
            AIFunctionFactory.Create(_systemTools.GetSystemInfo),
            AIFunctionFactory.Create(_httpTools.HttpRequest),
            AIFunctionFactory.Create(siteDataTools.RefreshInitialSiteData),
            AIFunctionFactory.Create(ctx.AskUser.AskUser),
        ];
    }
}
