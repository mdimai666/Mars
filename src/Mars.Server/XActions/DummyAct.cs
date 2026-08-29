using Mars.Data.Contexts;
using Mars.Contracts.XActions;
using Mars.Server.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
