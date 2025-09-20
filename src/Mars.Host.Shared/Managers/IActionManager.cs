using Mars.Shared.Contracts.XActions;

namespace Mars.Host.Shared.Managers;

public interface IActionManager
{
    Task RefreshDict(bool force = false);
    void AddAction(XActionCommand xAction);
    void AddXLink(XActionCommand xAction);
    void AddActionsProvider(IXActionCommandsProvider actionCommandsProvider);
    IReadOnlyDictionary<string, XActionCommand> XActions { get; }
    Task<XActResult> Inject(string id, string[] args, CancellationToken cancellationToken);
}

public interface IXActionCommandsProvider
{
    Task<IReadOnlyCollection<XActionCommand>> ReadCommands();
    Task<XActResult> RunCommand(XActionCommand action, string[] args);

}
