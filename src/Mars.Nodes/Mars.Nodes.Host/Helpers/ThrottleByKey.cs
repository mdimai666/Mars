using System.Collections.Concurrent;

namespace Mars.Nodes.Host.Helpers;

/// <summary>
/// Ограничивает частоту выполнения. Гарантирует, что действие выполнится не чаще чем раз в N времени.
/// </summary>
public class ThrottleByKey
{
    private readonly ConcurrentDictionary<string, ThrottleInfo> _throttleInfos = new();
    private readonly TimeSpan _minInterval;
    private readonly TimeSpan? _cleanupInterval;
    private DateTime _lastCleanup = DateTime.UtcNow;

    public ThrottleByKey(TimeSpan minInterval, TimeSpan? cleanupInterval = null)
    {
        _minInterval = minInterval;
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);
    }

    public bool TryExecute(string key, Action action)
    {
        if (ShouldThrottle(key))
            return false;

        action?.Invoke();
        return true;
    }

    public async Task<bool> TryExecuteAsync(string key, Func<Task> asyncAction)
    {
        if (ShouldThrottle(key))
            return false;

        if (asyncAction != null)
            await asyncAction();

        return true;
    }

    private bool ShouldThrottle(string key)
    {
        var info = _throttleInfos.GetOrAdd(key, _ => new ThrottleInfo());
        var now = DateTime.UtcNow;

        if ((now - info.LastExecutionTime) < _minInterval)
            return true;

        info.LastExecutionTime = now;
        TryCleanup();
        return false;
    }

    private void TryCleanup()
    {
        if (_cleanupInterval.HasValue &&
            (DateTime.UtcNow - _lastCleanup) > _cleanupInterval.Value)
        {
            Cleanup();
            _lastCleanup = DateTime.UtcNow;
        }
    }

    private void Cleanup()
    {
        var cutoff = DateTime.UtcNow - (_cleanupInterval ?? TimeSpan.FromMinutes(5));
        foreach (var kvp in _throttleInfos)
        {
            if (kvp.Value.LastExecutionTime < cutoff)
            {
                _throttleInfos.TryRemove(kvp.Key, out _);
            }
        }
    }

    private class ThrottleInfo
    {
        public DateTime LastExecutionTime { get; set; } = DateTime.MinValue;
    }
}
