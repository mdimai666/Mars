using Mars.AiChat.Host.Tools;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Ядро: настройки сайта, любые опции, информация о системе, HTTP-запросы, ask_user.
/// </summary>
public class CoreToolset : IAiToolset
{
    private readonly MarsSiteTools _siteTools;
    private readonly MarsOptionsTools _optionsTools;
    private readonly MarsSystemTools _systemTools;
    private readonly MarsHttpTools _httpTools;

    public CoreToolset(
        MarsSiteTools siteTools,
        MarsOptionsTools optionsTools,
        MarsSystemTools systemTools,
        MarsHttpTools httpTools)
    {
        _siteTools = siteTools;
        _optionsTools = optionsTools;
        _systemTools = systemTools;
        _httpTools = httpTools;
    }

    public string Name => "core";

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx) =>
    [
        AIFunctionFactory.Create(_siteTools.GetSiteSettings),
        AIFunctionFactory.Create(_siteTools.UpdateSiteSettings),
        AIFunctionFactory.Create(_optionsTools.ListSiteOptions),
        AIFunctionFactory.Create(_optionsTools.GetSiteOption),
        AIFunctionFactory.Create(_optionsTools.UpdateSiteOption),
        AIFunctionFactory.Create(_systemTools.GetSystemInfo),
        AIFunctionFactory.Create(_httpTools.HttpRequest),
        AIFunctionFactory.Create(ctx.AskUser.AskUser),
    ];
}
