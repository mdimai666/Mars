namespace Mars.Server.Abstractions.Managers;

public interface IEventManager
{
    public EventManagerDefaults Defaults { get; }

    public delegate void ManagerEventPayloadHandler(ManagerEventPayload payload);

    public event ManagerEventPayloadHandler OnTrigger;

    public void AddEventListener(string eventName, Action<ManagerEventPayload> listener);
    public void RemoveEventListener(string eventName, Action<ManagerEventPayload> listener);
    public void TriggerEvent(ManagerEventPayload payload);

    public static bool TestTopic(string topic, string value) => TopicMatcher.IsMatch(topic, value);

    public IReadOnlyCollection<KeyValuePair<string, string>> DeclaredEvents();
}

public class EventManagerDefaults
{

}

public class ManagerEventPayload : EventArgs
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Created { get; set; } = DateTime.Now;

    public object Data { get; set; }

    /// <summary>
    /// mars.entity.Post/news/add
    /// entity/news/add
    /// </summary>
    public string Topic { get; set; } = "";


    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic">entity.Post/news/add</param>
    /// <param name="data">Set only object copy. not relation</param>
    public ManagerEventPayload(string topic, object data)
    {
        Topic = topic;
        Data = data;
    }
}
