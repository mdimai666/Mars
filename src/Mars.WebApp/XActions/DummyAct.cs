using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;
using Microsoft.EntityFrameworkCore;

namespace Mars.XActions;

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

    public async Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var logger = MarsLogger.GetStaticLogger<DummyAct>();

        int count = await ef.Posts.CountAsync();

        var message = $"act executed. Post count = {count}";

        logger.LogWarning(message);

        return XActResult.ToastSuccess(message);
    }
}

#endif
