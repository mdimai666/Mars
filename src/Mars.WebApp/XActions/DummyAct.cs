using System.ComponentModel.Design;
using AppAdmin.Pages.Settings;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;
using Microsoft.EntityFrameworkCore;

namespace Mars.XActions;

#if DEBUG
[RegisterXActionCommand(CommandId, "Clear cache")]
public class DummyAct(MarsDbContext ef) : IAct
{
    public const string CommandId = "Mars.XActions." + nameof(DummyAct);

    public static XActionCommand XAction { get; } = new XActionCommand()
    {
        Id = CommandId,
        Label = "DummyAct",
        FrontContextId = [typeof(SettingsPage).FullName!],
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
