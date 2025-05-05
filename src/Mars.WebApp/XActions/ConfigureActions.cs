using System.Reflection;
#if !NOADMIN
using AppAdmin.Pages.FeedbackViews;
using AppAdmin.Pages.PostsViews;
using AppAdmin.Pages.Settings;
#endif
using Mars.Controllers;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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

#if false
        actionManager.AddXLink(new XActionCommand
        {
            Id = typeof(IRuntimeTypeCompiler).FullName + "+csharp",
            FrontContextId = [typeof(ManagePostTypePage).FullName],
            Label = "Просмотр кода C#",
            LinkValue = "/api/GenSourceCode/MetaModelsMto?lang=csharp"
        });

        actionManager.AddXLink(new XActionCommand
        {
            Id = typeof(DebugPage).FullName,
            FrontContextId = [typeof(SettingsPage).FullName, typeof(NodeRedPage).FullName],
            Label = "Логи",
            LinkValue = "builder/debug"
        }); 
#endif

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

public class ClearCacheAct : IAct
{
    private readonly IMemoryCache memoryCache;
    public static XActionCommand XAction { get; } = new XActionCommand()
    {
        Id = typeof(ClearCacheAct).FullName!,
        Label = "Очистить кеш",
#if !NOADMIN
        FrontContextId = [typeof(SettingsHostCachePage).FullName + "-wrapper"],
#endif
        Type = XActionType.HostAction
    };

    public ClearCacheAct(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }


    public Task<XActResult> Execute(IActContext context)
    {
        if (memoryCache is MemoryCache mc)
        {
            mc.Clear();
            return Task.FromResult(XActResult.ToastSuccess("cache clear"));
        }
        return Task.FromResult(XActResult.ToastError("clear cache error"));
    }
}

#if DEBUG
public class DummyAct(MarsDbContext ef) : IAct
{
    public static XActionCommand XAction { get; } = new XActionCommand()
    {
        Id = typeof(DummyAct).FullName!,
        Label = "DummyAct",
#if false
        FrontContextId = [typeof(EditPostPage).FullName], 
#endif
        Type = XActionType.HostAction
    };

    public async Task<XActResult> Execute(IActContext context)
    {
        var logger = MarsLogger.GetStaticLogger<DummyAct>();

        int count = await ef.Posts.CountAsync();

        var message = $"act executed. Post count = {count}";

        logger.LogWarning(message);

        return XActResult.ToastSuccess(message);
    }
}
#endif
