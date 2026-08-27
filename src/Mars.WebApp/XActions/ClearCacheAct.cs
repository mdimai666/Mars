using Mars.Contracts.XActions;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.XActions;

public class ClearCacheAct : IAct
{
    public const string CommandId = "mars.host.clearCache";
    private readonly IMemoryCache _memoryCache;

    public ClearCacheAct(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        if (_memoryCache is MemoryCache mc)
        {
            mc.Clear();
            return Task.FromResult(XActResult.ToastSuccess("cache clear"));
        }
        return Task.FromResult(XActResult.ToastError("clear cache error"));
    }
}
