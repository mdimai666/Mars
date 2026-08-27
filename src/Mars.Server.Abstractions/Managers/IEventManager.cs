namespace Mars.Host.Shared.Managers;

public interface IEventManager
{
    public EventManagerDefaults Defaults { get; }

    public delegate void ManagerEventPayloadHandler(ManagerEventPayload payload);

    public event ManagerEventPayloadHandler OnTrigger;

    public void AddEventListener(string eventName, Action<ManagerEventPayload> listener);
    public void RemoveEventListener(string eventName, Action<ManagerEventPayload> listener);
    public void TriggerEvent(ManagerEventPayload payload);

    public static bool TestTopic(string topic, string value)
    {
        if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(value)) return false;
        if (topic == "*" || topic.Equals(value)) return true;

        var topicSegments = topic.ToLower().Split('/');
        var valueSegments = value.ToLower().Split('/');

        for (var i = 0; i < topicSegments.Count(); i++)
        {
            var seg = topicSegments[i];
            var v = valueSegments.ElementAtOrDefault(i);
            if (v == default) return false;

            if (seg.StartsWith('['))
            {
                var segArr = seg.Substring(1, seg.Length - 2).Split(',', StringSplitOptions.TrimEntries);
                if (segArr.Contains(v)) continue;
                else return false;
            }
            else
            {
                if (seg == "*" || seg.Equals(v)) continue;
                else if (seg == "**" && i == topicSegments.Count() - 1) return true;
                else return false;
            }
        }
        if (valueSegments.Count() > topicSegments.Count()) return false;
        return true;
    }

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
