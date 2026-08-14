using System.Collections.Concurrent;
using System.Text.Json;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Parsing;

namespace Mars.PxBlocks.Host.Services;

/// <summary>
/// Оркестратор запусков: разбор синхронно (ошибка — сразу в ответ), исполнение —
/// фоновая задача; события пакетируются и стримятся через IPxBlocksBroadcaster,
/// Stop отзывает CancellationToken интерпретатора. Параллельные запуски допустимы.
/// Программа продолжает работать после отключения клиента (серверный сценарий).
/// </summary>
public sealed class PxRunManager : IPxRunManager
{
    private readonly IPxBlockCatalog _catalog;
    private readonly IPxBlocksBroadcaster _broadcaster;
    private readonly ConcurrentDictionary<Guid, PxRunSession> _runs = new();

    public PxRunManager(IPxBlockCatalog catalog, IPxBlocksBroadcaster broadcaster)
    {
        _catalog = catalog;
        _broadcaster = broadcaster;
    }

    public int ActiveRunCount => _runs.Count;

    public PxRunResponse Start(PxRunRequest request)
    {
        PxProgram program;
        try
        {
            program = new PxParser(_catalog.Implements).Parse(request.BlocksJson);
        }
        catch (PxParseException exception)
        {
            return new PxRunResponse
            {
                Started = false,
                ErrorMessage = exception.Message,
                ErrorBlockId = exception.BlockId
            };
        }
        catch (JsonException exception)
        {
            return new PxRunResponse
            {
                Started = false,
                ErrorMessage = $"Некорректный JSON workspace: {exception.Message}"
            };
        }

        // RunId назначает клиент (подписывается на события до запроса Run); Empty — сами.
        var runId = request.RunId == Guid.Empty ? Guid.NewGuid() : request.RunId;
        var session = new PxRunSession(runId, _broadcaster);
        if (!_runs.TryAdd(runId, session))
        {
            session.Dispose();
            return new PxRunResponse { Started = false, ErrorMessage = $"Запуск {runId} уже активен" };
        }

        _ = Task.Run(() => ExecuteAsync(session, program, request));
        return new PxRunResponse { RunId = runId, Started = true };
    }

    public bool Stop(Guid runId)
    {
        if (_runs.TryGetValue(runId, out var session))
        {
            session.Cts.Cancel();
            return true;
        }

        return false;
    }

    private async Task ExecuteAsync(PxRunSession session, PxProgram program, PxRunRequest request)
    {
        var options = new PxRunOptions
        {
            StepLimit = request.StepLimit,
            OutputLimit = request.OutputLimit,
            RandomSeed = request.RandomSeed,
            EventNames = request.EventNames,
            OnEvent = session.Enqueue
        };

        PxExecutionResult result;
        try
        {
            var interpreter = new PxInterpreter(_catalog.Implements);
            result = await interpreter.RunAsync(program, options, session.Cts.Token);
        }
        catch (Exception exception)
        {
            result = new PxExecutionResult { Success = false, ErrorMessage = exception.Message };
        }

        await session.CompleteAsync(new PxRunResultDto
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            ErrorBlockId = result.ErrorBlockId,
            Canceled = result.Canceled,
            Steps = result.Steps
        });

        _runs.TryRemove(session.RunId, out _);
        session.Dispose();
    }
}
