using Mars.XActions.Abstractions.Managers;
using Mars.XActions.Contracts;

namespace StandNodesApp.Mocks;

internal class ActionManagerMock : IActionManager
{
    public IReadOnlyDictionary<string, XActionCommand> XActions { get; } = new Dictionary<string, XActionCommand>();

    public void Add(Action<XActionBuilder> configure)
    {
        //throw new NotImplementedException();
    }

    public void AddFrontContexts(string id, params string[] frontContexts)
    {
        //throw new NotImplementedException();
    }

    public void AddActionsProvider(IXActionCommandsProvider actionCommandsProvider)
    {
        //throw new NotImplementedException();
    }

    public void AddOptionsSource(string key, Func<CancellationToken, Task<IReadOnlyCollection<XActionOption>>> factory)
    {
        //throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<XActionOption>> GetOptionsAsync(string sourceKey, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<XActionOption>>([]);
    }

    public Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken)
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
