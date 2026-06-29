using System.Collections.Concurrent;

namespace Mars.Nodes.Host.Helpers;

/// <summary>
/// debounce на 50мс для первого сообщения + throttle на 300мс для последующих. Это классический паттерн для UI, чтобы сгруппировать быстрые клики, но не перегружать систему.
/// </summary>
public class SmartThrottleByKey
{
    private readonly ConcurrentDictionary<string, ThrottleState> _states = new();
    private readonly TimeSpan _initialDelay;  // 50ms - debounce для первого
    private readonly TimeSpan _throttleDelay; // 300ms - throttle для остальных

    public SmartThrottleByKey(TimeSpan throttleDelay, TimeSpan? initialDelay = null)
    {
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(50);
        _throttleDelay = throttleDelay;
    }

    public async Task<bool> TryExecuteAsync(string key, Func<Task> action)
    {
        if (action == null) return false;

        var state = _states.GetOrAdd(key, _ => new ThrottleState());

        lock (state)
        {
            state.PendingAction = action;

            switch (state.Phase)
            {
                case Phase.Idle:
                    // Первое сообщение - запускаем debounce
                    state.Phase = Phase.Debouncing;
                    state.Cts = new CancellationTokenSource();
                    _ = RunDebounceAsync(key, state);
                    break;

                case Phase.Debouncing:
                    // Уже ждём - отменяем старый delay, запускаем новый
                    state.Cts?.Cancel();
                    state.Cts = new CancellationTokenSource();
                    _ = RunDebounceAsync(key, state);
                    break;

                case Phase.Throttling:
                    // В режиме throttle - просто запоминаем действие
                    // Оно выполнится после окончания throttle периода
                    break;
            }
        }

        return true;
    }

    public void TryExecute(string key, Action action)
    {
        if (action == null) return;
        _ = TryExecuteAsync(key, () => { action(); return Task.CompletedTask; });
    }

    private async Task RunDebounceAsync(string key, ThrottleState state)
    {
        try
        {
            await Task.Delay(_initialDelay, state.Cts.Token);
        }
        catch (TaskCanceledException)
        {
            return; // Отменён новым вызовом
        }

        // Debounce закончился - выполняем действие
        Func<Task> actionToExecute;
        lock (state)
        {
            actionToExecute = state.PendingAction!;
            state.PendingAction = null;
            state.Phase = Phase.Throttling;
            state.Cts = new CancellationTokenSource();
        }

        if (actionToExecute != null)
        {
            await actionToExecute();
        }

        // Запускаем throttle период
        _ = RunThrottleAsync(key, state);
    }

    private async Task RunThrottleAsync(string key, ThrottleState state)
    {
        try
        {
            await Task.Delay(_throttleDelay, state.Cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        // Throttle закончился - проверяем, есть ли ожидающее действие
        Func<Task>? pendingAction;
        lock (state)
        {
            pendingAction = state.PendingAction;

            if (pendingAction != null)
            {
                // Есть ожидающее - выполняем и запускаем новый throttle
                state.PendingAction = null;
                state.Cts = new CancellationTokenSource();
                _ = RunThrottleAsync(key, state);
            }
            else
            {
                // Нет ожидающих - переходим в Idle
                state.Phase = Phase.Idle;
                state.Cts?.Dispose();
                state.Cts = null;
            }
        }

        if (pendingAction != null)
        {
            await pendingAction();
        }
    }

    private enum Phase
    {
        Idle,        // Ожидание первого сообщения
        Debouncing,  // Ждём 50ms после первого сообщения
        Throttling   // Ждём 300ms после выполнения
    }

    private class ThrottleState
    {
        public Phase Phase { get; set; } = Phase.Idle;
        public Func<Task>? PendingAction { get; set; }
        public CancellationTokenSource? Cts { get; set; }
    }
}
