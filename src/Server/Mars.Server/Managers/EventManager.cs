using System.Collections.Immutable;
using Mars.Server.Abstractions.Managers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.Server.Managers;

/// <summary>
/// Singleton service. Thread-safe: subscriptions live in an immutable snapshot (copy-on-write);
/// <see cref="TriggerEvent"/> dispatches lock-free against the snapshot it read.
/// </summary>
internal sealed class EventManager : IEventManager
{
    private sealed record Subscription(CompiledTopic Topic, Action<ManagerEventPayload> Handler);

    private sealed class RouteTable
    {
        public static readonly RouteTable Empty = new([]);

        public ImmutableArray<Subscription> All { get; }
        public ImmutableDictionary<string, ImmutableArray<Subscription>> ByStartSegment { get; }
        public ImmutableArray<Subscription> CatchAll { get; }

        public RouteTable(ImmutableArray<Subscription> all)
        {
            All = all;
            ByStartSegment = all
                .Where(s => s.Topic.StartSegment is not null)
                .GroupBy(s => s.Topic.StartSegment!, StringComparer.Ordinal)
                .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray(), StringComparer.Ordinal);
            CatchAll = all.Where(s => s.Topic.StartSegment is null).ToImmutableArray();
        }
    }

    private readonly object _sync = new();
    private readonly ILogger<EventManager> _logger;
    private volatile RouteTable _routes = RouteTable.Empty;

    public EventManager(ILogger<EventManager>? logger = null) =>
        _logger = logger ?? NullLogger<EventManager>.Instance;

    public event IEventManager.ManagerEventPayloadHandler OnTrigger = default!;

    public EventManagerDefaults Defaults { get; } = new();

    public void AddEventListener(string eventName, Action<ManagerEventPayload> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        var subscription = new Subscription(TopicMatcher.Compile(eventName), listener);
        lock (_sync)
        {
            _routes = new RouteTable(_routes.All.Add(subscription));
        }
    }

    public void RemoveEventListener(string eventName, Action<ManagerEventPayload> listener)
    {
        lock (_sync)
        {
            _routes = new RouteTable(_routes.All.RemoveAll(s =>
                s.Topic.Pattern == eventName && s.Handler == listener));
        }
    }

    public void TriggerEvent(ManagerEventPayload payload)
    {
        _logger.LogTrace("TriggerEvent {Topic}", payload.Topic);

        var routes = _routes;
        var start = StartSegment(payload.Topic);

        if (start is not null && routes.ByStartSegment.TryGetValue(start, out var group))
        {
            Dispatch(group, payload);
        }
        Dispatch(routes.CatchAll, payload);

        OnTrigger?.Invoke(payload);
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> DeclaredEvents() =>
        _routes.All
            .Select(s => s.Topic.Pattern)
            .Distinct(StringComparer.Ordinal)
            .Select(t => new KeyValuePair<string, string>(t, t))
            .ToList();

    private void Dispatch(ImmutableArray<Subscription> subscriptions, ManagerEventPayload payload)
    {
        foreach (var subscription in subscriptions)
        {
            if (!subscription.Topic.IsMatch(payload.Topic)) continue;

            try
            {
                subscription.Handler(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event listener for topic {Topic} failed", payload.Topic);
            }
        }
    }

    private static string? StartSegment(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic)) return null;
        var i = topic.IndexOf('/');
        return (i < 0 ? topic : topic[..i]).ToLowerInvariant();
    }
}
