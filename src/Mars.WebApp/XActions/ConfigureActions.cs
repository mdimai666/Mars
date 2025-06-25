using System.Reflection;
using AppAdmin.Builder.DebugViews;
using AppAdmin.Builder.NodeViews;

#if !NOADMIN
using AppAdmin.Pages.FeedbackViews;
using AppAdmin.Pages.PostsViews;
using AppAdmin.Pages.PostTypeViews;
using AppAdmin.Pages.Settings;
#endif
using Mars.Controllers;
using Mars.Host.Shared.Managers;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Resources;

namespace Mars.XActions;

internal static class ConfigureActions
{
    static Type[] actTypes = default!;

    public static IServiceCollection AddConfigureActions(this IServiceCollection services)
    {
        Assembly[] assemblies = [typeof(Program).Assembly];

        actTypes = assemblies.SelectMany(assembly => assembly.GetTypes().Where(s => typeof(IAct).IsAssignableFrom(s))).ToArray();

        foreach (var type in actTypes)
        {
            services.AddTransient(type);
        }

        return services;
    }

    public static IApplicationBuilder UseConfigureActions(this WebApplication app)
    {
        IActionManager actionManager = app.Services.GetRequiredService<IActionManager>();

        foreach (var type in actTypes)
            actionManager.AddAction(type);
        //actionManager.AddAction<DummyAct>();
        //actionManager.AddAction<ClearCacheAct>();

#if !NOADMIN
        actionManager.AddXLink(new XActionCommand
        {
            Id = "App.Logs",
            FrontContextId = [typeof(SettingsAboutSystemPage).FullName!],
            Label = "App logs",
            LinkValue = "/dev/builder/debug"
        });

#if DEBUG
        actionManager.AddXLink(new XActionCommand
        {
            Id = "empty1",
            FrontContextId = [typeof(ManagePostPage).FullName + "-post"],
            Label = "EmptyAct 1",
            LinkValue = "@empty"
        });

        actionManager.AddXLink(new XActionCommand
        {
            Id = typeof(EditPostPage).FullName + "-page",
            FrontContextId = [typeof(EditPostPage).FullName + "-page"],
            Label = "test",
            LinkValue = "/{page_slug}"
        });
#endif
#endif

        actionManager.AddXLink(new XActionCommand
        {
            Id = nameof(GenSourceCodeController.MetaTypesSourceCode) + "+csharp",
            FrontContextId = [typeof(ListPostTypePage).FullName!],
            Label = "Просмотр кода C#",
            LinkValue = $"/api/GenSourceCode/{nameof(GenSourceCodeController.MetaTypesSourceCode)}?lang=csharp"
        });

        actionManager.AddXLink(new XActionCommand
        {
            Id = typeof(DebugPage).FullName!,
            FrontContextId = [typeof(SettingsPage).FullName!, typeof(NodeRedPage).FullName!],
            Label = "Логи",
            LinkValue = "builder/debug"
        });

#if !NOADMIN
        actionManager.AddXLink(new XActionCommand
        {
            Id = "Feedback.DownloadExcelList",
            FrontContextId = [typeof(FeedbackListPage).FullName!],
            Label = AppRes.DownloadExcel,
            LinkValue = $"/api/Feedback/{nameof(FeedbackController.DownloadExcel)}"
        });
#endif

        return app;
    }

}
