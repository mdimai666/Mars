#if !NOADMIN
using Mars.Admin.Builder.DebugViews;
using Mars.Admin.Builder.NodeViews;
using Mars.Admin.Pages.PostsViews;
using Mars.Admin.Pages.PostTypeViews;
using Mars.Admin.Pages.Settings;
#endif
using Mars.Server.Abstractions.Managers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Admin.Host;

/// <summary>
/// Оверлей привязки XActions к страницам админки: команды регистрируют доменные
/// владельцы (Cms.Host, Mars.Server) без контекстов, а контексты и админские
/// ссылки-шорткаты заявляются здесь — единственном месте, знающем типы страниц.
/// </summary>
internal static class AdminXActionsOverlay
{
    public static IApplicationBuilder UseMarsAdminXActions(this WebApplication app)
    {
#if NOADMIN
        return app;
#endif

        var actionManager = app.Services.GetRequiredService<IActionManager>();

        actionManager.AddFrontContexts("mars.host.clearCache", typeof(SettingsHostCachePage).FullName!);
        actionManager.AddFrontContexts("mars.content.createMockPosts", typeof(ManagePostPage).FullName + "-post");
        actionManager.AddFrontContexts("mars.content.templates.createPresentation", typeof(EditPostTypePresentationPage).FullName!);
        // корневой контекст страницы списка постов (без суффикса типа) — видно для всех типов
        // в дропдауне «Действия» (он с AlsoShowRootContext) и при любом {тип} в маршруте
        actionManager.AddFrontContexts("mars.content.regenerateGeneratedMetaValues", typeof(ManagePostPage).FullName!);
        actionManager.AddFrontContexts("App.Logs", typeof(SettingsAboutSystemPage).FullName!);

        actionManager.Add(a => a
            .Id(typeof(DebugPage).FullName!)
            .Label("Логи")
            .Category("Разработка")
            .Link("builder/debug")
            .FrontContexts(typeof(SettingsPage).FullName!, typeof(NodeRedPage).FullName!));

#if DEBUG
        actionManager.AddFrontContexts("mars.debug.dummy", typeof(SettingsPage).FullName!);

        actionManager.Add(a => a
            .Id("Mars.Debug.EmptyLink")
            .Label("EmptyAct 1")
            .Category("Отладка")
            .FrontContexts(typeof(ManagePostPage).FullName + "-post")
            .Link("@empty"));

        actionManager.Add(a => a
            .Id(typeof(EditPostPage).FullName + "-page")
            .Label("test")
            .Category("Отладка")
            .FrontContexts(typeof(EditPostPage).FullName + "-page")
            .Link("/{page_slug}"));
#endif

        actionManager.RefreshDict();

        return app;
    }
}
