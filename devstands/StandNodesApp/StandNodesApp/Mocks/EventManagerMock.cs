using Mars.Host.Shared.Managers;

namespace StandNodesApp.Mocks;

internal class EventManagerMock : IEventManager
{
    public EventManagerDefaults Defaults { get; } = default!;

    public event IEventManager.ManagerEventPayloadHandler OnTrigger = default!;

    public void AddEventListener(string eventName, Action<ManagerEventPayload> listener)
    {
        //throw new NotImplementedException();
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> DeclaredEvents()
    {
        //throw new NotImplementedException();
        return [];
    }

    public void RemoveEventListener(string eventName, Action<ManagerEventPayload> listener)
    {
        //throw new NotImplementedException();
    }

    public void TriggerEvent(ManagerEventPayload payload)
    {
        //throw new NotImplementedException();
    }
}
