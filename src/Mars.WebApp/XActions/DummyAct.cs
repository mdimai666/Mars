using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;
using Microsoft.EntityFrameworkCore;

namespace Mars.XActions;

#if DEBUG
public class DummyAct(MarsDbContext ef) : IAct
{
    public const string CommandId = "mars.debug.dummy";

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var logger = MarsLogger.GetStaticLogger<DummyAct>();

        int count = await ef.Posts.CountAsync();

        var message = $"act executed. Post count = {count}";

        logger.LogWarning(message);

        return XActResult.ToastSuccess(message)
            .WithNavigate("/dev")
            .WithEvent("dummy-act-executed");
    }
}

#endif
