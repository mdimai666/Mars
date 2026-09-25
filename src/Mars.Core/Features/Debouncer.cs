public class Debouncer : IDisposable
{
    private readonly int _millisecondsToWait;
    private readonly object _lockThis = new();
    private CancellationTokenSource? _cts;

    public Debouncer(int millisecondsToWait = 300)
    {
        _millisecondsToWait = millisecondsToWait;
    }

    public void Debounce(Action func)
    {
        CancellationToken token;
        lock (_lockThis)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            token = _cts.Token;
        }
        _ = FireAsync(func, token);
    }

    private async Task FireAsync(Action func, CancellationToken token)
    {
        try
        {
            await Task.Delay(_millisecondsToWait, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        lock (_lockThis)
        {
            if (token.IsCancellationRequested) return;
            func();
        }
    }

    public void Dispose()
    {
        lock (_lockThis)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
