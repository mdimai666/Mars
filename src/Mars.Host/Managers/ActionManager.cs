using System.Reflection;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Host.Shared.Managers;
using Mars.Shared.Contracts.XActions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Host.Managers;

/// <summary>
/// Singletone service
/// </summary>
internal class ActionManager(IServiceProvider rootServiceProvider, ILogger<ActionManager> logger) : IActionManager
{
    Dictionary<string, XActionCommand> acts = [];

    public void AddAction<TAct>(XActionCommand? xAction = null) where TAct : IAct => AddAction(typeof(TAct));
    public void AddAction(Type actType, XActionCommand? xAction = null)
    {
        XActionCommand? actionPropValue = null;
        if (xAction is null)
        {
            var actionProp = actType.GetProperty("XAction", BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty);
            if (actionProp is not null)
            {
                actionPropValue = (XActionCommand)actionProp.GetValue(null, null)!;
            }
        }

        XActionCommand cmd = xAction ?? actionPropValue ?? new XActionCommand()
        {
            Id = actType.FullName!,
            Label = actType.Name,
            Type = XActionType.HostAction,
        };

        if (cmd.Type != XActionType.HostAction) throw new ArgumentException("not valid type");

        acts.Add(cmd.Id, new XActionCommandImpl(cmd, actType));
    }

    public void AddXLink(XActionCommand xAction)
    {
        if (xAction.Type != XActionType.Link) throw new ArgumentException("not valid type");
        if (string.IsNullOrEmpty(xAction.LinkValue)) throw new ArgumentNullException("LinkValue cannot be empty for link");
        acts.Add(xAction.Id, xAction);
    }

    public IReadOnlyDictionary<string, XActionCommand> XActions => acts;

    public async Task<XActResult> Inject(string id, string[] args, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!acts.TryGetValue(id, out var val)) throw new NotFoundException("ActionManager: action not found");

        if (val is XActionCommandImpl hostAction)
        {
            using var scope = rootServiceProvider.CreateScope();
            var act = scope.ServiceProvider.GetRequiredService(hostAction.TAct) as IAct;

            try
            {
                var context = new ActContext { args = args };
                logger.LogInformation($"Inject: '{act.GetType().FullName}'. args='{args.JoinStr(",")}'");
                return await act.Execute(context, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return XActResult.ToastError("ActionManager: " + ex.Message);
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}

internal record ActContext : IActContext
{
    public string[] args { get; init; } = [];
}

public class XActionCommandImpl : XActionCommand
{
    public Type TAct { get; init; }

    public XActionCommandImpl(XActionCommand xAction, Type tAct)
    {
        Id = xAction.Id;
        Label = xAction.Label;
        Type = xAction.Type;
        KeybindingContext = xAction.KeybindingContext;
        Keybindings = xAction.Keybindings;
        ContextMenuGroupId = xAction.ContextMenuGroupId;
        ContextMenuOrder = xAction.ContextMenuOrder;
        FrontContextId = xAction.FrontContextId;

        TAct = tAct;
    }
}
