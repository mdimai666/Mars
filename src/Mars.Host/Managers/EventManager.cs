using Mars.Host.Shared.Managers;

namespace Mars.Host.Managers;

/// <summary>
/// Singletone service
/// </summary>
internal class EventManager : IEventManager
{
    Dictionary<string, List<Action<ManagerEventPayload>>> _events = new();

    // <startSegment<topic,action>>
    Dictionary<string, Dictionary<string, List<Action<ManagerEventPayload>>>> _groupedByStartSegmentEvents = new();

    //public delegate void ManagerEventPayloadHandler(ManagerEventPayload payload);

    public event Shared.Managers.IEventManager.ManagerEventPayloadHandler OnTrigger;

    EventManagerDefaults _defaults = new();
    public EventManagerDefaults Defaults => _defaults;

    void RecalcGroup()
    {
        _groupedByStartSegmentEvents = _events.GroupBy(s => StartSegment(s.Key))
            .ToDictionary(s => s.Key, s => s.ToDictionary());
    }

    public void AddEventListener(string eventName, Action<ManagerEventPayload> listener)
    {
        if (!_events.TryGetValue(eventName, out var listeners))
        {
            listeners = new List<Action<ManagerEventPayload>>();
            _events.Add(eventName, listeners);
        }

        listeners.Add(listener);
        RecalcGroup();
    }

    public void RemoveEventListener(string eventName, Action<ManagerEventPayload> listener)
    {
        if (_events.TryGetValue(eventName, out var listeners))
        {
            listeners.Remove(listener);

            if (listeners.Count == 0)
            {
                _events.Remove(eventName);
            }
        }
        RecalcGroup();
    }

    public void TriggerEvent(ManagerEventPayload payload)
    {
#if DEBUG
        Console.WriteLine($">TriggerEvent={payload.Topic}");
#endif

        if (_groupedByStartSegmentEvents.TryGetValue(StartSegment(payload.Topic), out var group))
        {
            foreach (var topic in group.Keys)
            {
                bool isMatch = IEventManager.TestTopic(topic, payload.Topic);

                if (isMatch)
                {
                    List<Action<ManagerEventPayload>> listeners = group[topic];

                    foreach (var listener in listeners)
                    {
                        try
                        {
                            listener(payload);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"TriggerEvent={payload.Topic} " + ex.Message);
                            //throw;
                        }
                    }
                }
            }
        }

        OnTrigger?.Invoke(payload);
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> DeclaredEvents()
    {
        return _events.Select(s => s.Key).Select(s => new KeyValuePair<string, string>(s, s)).ToList();
    }

    static string StartSegment(string topic)
    {
        var ss = topic.Split('/');
        return ss[0];
    }
}

