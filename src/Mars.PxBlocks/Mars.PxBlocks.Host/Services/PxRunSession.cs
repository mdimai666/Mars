using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Execution;

namespace Mars.PxBlocks.Host.Services;

/// <summary>
/// Состояние одного запуска: буфер событий с таймером пакетирования (100 мс или
/// порог размера), отправка — цепочкой последовательных задач, чтобы RunEvents
/// и RunFinished пришли клиенту строго по порядку.
/// </summary>
internal sealed class PxRunSession : IDisposable
{
    internal const int BatchSize = 256;
    internal static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(100);

    private readonly IPxBlocksBroadcaster _broadcaster;
    private readonly object _gate = new();
    private List<PxExecutionEvent> _buffer = [];
    private Timer? _timer;
    private Task _sendChain = Task.CompletedTask;
    private bool _completed;

    public PxRunSession(Guid runId, IPxBlocksBroadcaster broadcaster)
    {
        RunId = runId;
        _broadcaster = broadcaster;
    }

    public Guid RunId { get; }

    public CancellationTokenSource Cts { get; } = new();

    /// <summary>Событие интерпретатора — в буфер; переполнение буфера шлёт пакет сразу.</summary>
    public void Enqueue(PxExecutionEvent executionEvent)
    {
        lock (_gate)
        {
            if (_completed)
                return;

            _buffer.Add(executionEvent);
            if (_buffer.Count >= BatchSize)
            {
                FlushLocked();
            }
            else
            {
                _timer ??= new Timer(OnTimer, null, FlushInterval, FlushInterval);
            }
        }
    }

    /// <summary>Дослать остаток буфера и итог; завершает приём событий.</summary>
    public Task CompleteAsync(PxRunResultDto result)
    {
        Task done;
        lock (_gate)
        {
            _completed = true;
            _timer?.Dispose();
            _timer = null;

            FlushLocked();
            ChainSend(() => _broadcaster.RunFinished(RunId, result));
            done = _sendChain;
        }

        return done;
    }

    public void Dispose() => Cts.Dispose();

    private void OnTimer(object? state)
    {
        lock (_gate)
        {
            if (!_completed)
                FlushLocked();
        }
    }

    /// <summary>Под замком: забрать буфер и поставить отправку в цепочку.</summary>
    private void FlushLocked()
    {
        if (_buffer.Count == 0)
            return;

        var batch = _buffer;
        _buffer = [];
        ChainSend(() => _broadcaster.RunEvents(RunId, batch));
    }

    /// <summary>Отправки идут последовательно; сбой одной (клиент ушёл) не рвёт цепочку.</summary>
    private void ChainSend(Func<Task> send)
    {
        _sendChain = _sendChain
            .ContinueWith(_ => SendIgnoringErrorsAsync(send), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default)
            .Unwrap();
    }

    private static async Task SendIgnoringErrorsAsync(Func<Task> send)
    {
        try
        {
            await send();
        }
        catch
        {
            // Подписчиков может уже не быть — события исполнения не критичны.
        }
    }
}
