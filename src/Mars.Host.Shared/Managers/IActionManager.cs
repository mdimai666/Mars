using Mars.Shared.Contracts.XActions;

namespace Mars.Host.Shared.Managers;

public interface IActionManager
{
    public void AddAction<TAct>(XActionCommand? xAction = null) where TAct : IAct;
    public void AddAction(Type actType, XActionCommand? xAction = null);
    public void AddXLink(XActionCommand xAction);
    public IReadOnlyDictionary<string, XActionCommand> XActions { get; }
    public Task<XActResult> Inject(string id, string[] args);
}