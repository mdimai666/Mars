#if !NOADMIN
using AppAdmin.Pages.Settings;
#endif
using Mars.Shared.Contracts.XActions;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.XActions;

public class ClearCacheAct : IAct
{
    private readonly IMemoryCache memoryCache;
    public static XActionCommand XAction { get; } = new XActionCommand()
    {
        Id = typeof(ClearCacheAct).FullName!,
        Label = "Очистить кеш",
#if !NOADMIN
        FrontContextId = [typeof(SettingsHostCachePage).FullName + "-post"],
#endif
        Type = XActionType.HostAction
    };

    public ClearCacheAct(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        if (memoryCache is MemoryCache mc)
        {
            mc.Clear();
            return Task.FromResult(XActResult.ToastSuccess("cache clear"));
        }
        return Task.FromResult(XActResult.ToastError("clear cache error"));
    }
}
