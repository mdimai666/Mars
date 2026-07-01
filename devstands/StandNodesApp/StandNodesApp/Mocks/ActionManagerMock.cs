using Mars.Host.Shared.Managers;
using Mars.Shared.Contracts.XActions;

namespace StandNodesApp.Mocks;

internal class ActionManagerMock : IActionManager
{
    public IReadOnlyDictionary<string, XActionCommand> XActions { get; } = new Dictionary<string, XActionCommand>();

    public void AddAction(XActionCommand xAction)
    {
        //throw new NotImplementedException();
    }

    public void AddActionsProvider(IXActionCommandsProvider actionCommandsProvider)
    {
        //throw new NotImplementedException();
    }

    public void AddXLink(XActionCommand xAction)
    {
        //throw new NotImplementedException();
    }

    public Task<XActResult> Inject(string id, string[] args, CancellationToken cancellationToken)
    {
        //throw new NotImplementedException();
        return Task.FromResult(XActResult.ToastSuccess("none"));
    }

    public Task RefreshDict(bool force = false)
    {
        //throw new NotImplementedException();
        return Task.CompletedTask;
    }
}
