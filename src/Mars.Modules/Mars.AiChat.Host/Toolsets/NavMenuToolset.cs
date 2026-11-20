using Mars.AiChat.Host.Tools;
using Mars.Cms.Abstractions.Services;
using Microsoft.Extensions.AI;

namespace Mars.AiChat.Host.Toolsets;

/// <summary>
/// Меню навигации CMS (в т.ч. сайдбар админки — меню slug "dev"); включён всегда.
/// Правила работы — в скилле mars-admin-pages.
/// </summary>
public class NavMenuToolset : IAiToolset
{
    private readonly INavMenuService _navMenuService;

    public NavMenuToolset(INavMenuService navMenuService)
    {
        _navMenuService = navMenuService;
    }

    public string Name => "nav-menus";

    public IReadOnlyList<AIFunction> Build(AiToolsetContext ctx)
    {
        var navMenuTools = new MarsNavMenuTools(_navMenuService);
        return
        [
            AIFunctionFactory.Create(navMenuTools.ListNavMenus),
            AIFunctionFactory.Create(navMenuTools.GetNavMenu),
            AIFunctionFactory.Create(navMenuTools.AddMenuItem),
            AIFunctionFactory.Create(navMenuTools.UpdateMenuItem),
            AIFunctionFactory.Create(navMenuTools.RemoveMenuItem),
        ];
    }
}