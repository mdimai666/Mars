using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Parsing;
using Mars.PxBlocks.Runtime.Values;

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
    private readonly IPxEditorContextRegistry _contexts;
    private readonly ConcurrentDictionary<Guid, PxRunSession> _runs = new();

    public PxRunManager(IPxBlockCatalog catalog, IPxBlocksBroadcaster broadcaster, IPxEditorContextRegistry contexts)
    {
        _catalog = catalog;
        _broadcaster = broadcaster;
        _contexts = contexts;
    }

    public int ActiveRunCount => _runs.Count;

    public PxRunResponse Start(PxRunRequest request, object? state = null)
    {
        // Политика запуска из контекста (режим событий, лимиты); неизвестный контекст —
        // ошибка сразу, как и ошибка разбора. Состояние запуска в обоих случаях
        // возвращается хосту через dispose — запуск не состоялся.
        PxEditorContext? context = null;
        if (request.ContextName != null)
        {
            context = _contexts.Get(request.ContextName);
            if (context == null)
            {
                DisposeState(state);
                return new PxRunResponse
                {
                    Started = false,
                    ErrorMessage = $"Контекст «{request.ContextName}» не зарегистрирован"
                };
            }
        }

        PxProgram program;
        try
        {
            program = new PxParser(_catalog.Implements).Parse(request.BlocksJson);
        }
        catch (PxParseException exception)
        {
            DisposeState(state);
            return new PxRunResponse
            {
                Started = false,
                ErrorMessage = exception.Message,
                ErrorBlockId = exception.BlockId
            };
        }
        catch (JsonException exception)
        {
            DisposeState(state);
            return new PxRunResponse
            {
                Started = false,
                ErrorMessage = $"Некорректный JSON workspace: {exception.Message}"
            };
        }

        IReadOnlyDictionary<string, PxValue>? initialVariables;
        try
        {
            initialVariables = ConvertInitialVariables(request.InitialVariables);
        }
        catch (Exception exception)
        {
            DisposeState(state);
            return new PxRunResponse
            {
                Started = false,
                ErrorMessage = $"Начальные переменные: {exception.Message}"
            };
        }

        // RunId назначает клиент (подписывается на события до запроса Run); Empty — сами.
        var runId = request.RunId == Guid.Empty ? Guid.NewGuid() : request.RunId;
        var session = new PxRunSession(runId, _broadcaster);
        if (!_runs.TryAdd(runId, session))
        {
            session.Dispose();
            DisposeState(state);
            return new PxRunResponse { Started = false, ErrorMessage = $"Запуск {runId} уже активен" };
        }

        _ = Task.Run(() => ExecuteAsync(session, program, request, context, state, initialVariables));
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

    private async Task ExecuteAsync(
        PxRunSession session,
        PxProgram program,
        PxRunRequest request,
        PxEditorContext? context,
        object? state,
        IReadOnlyDictionary<string, PxValue>? initialVariables)
    {
        // Явные поля запроса имеют приоритет; незаполненные дополняет политика контекста.
        var options = new PxRunOptions
        {
            StepLimit = request.StepLimit ?? context?.StepLimit ?? 0,
            OutputLimit = request.OutputLimit ?? context?.OutputLimit ?? 10_000,
            RandomSeed = request.RandomSeed,
            EventNames = request.EventNames ?? context?.EventNames,
            State = state,
            InitialVariables = initialVariables,
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
        finally
        {
            // Состояние запуска живёт ровно до конца исполнения — успех, ошибка или Stop.
            await DisposeStateAsync(state);
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

    /// <summary>JSON-значения запроса → PxValue (PxValueJson); null-значения — Null.</summary>
    private static IReadOnlyDictionary<string, PxValue>? ConvertInitialVariables(
        IReadOnlyDictionary<string, JsonNode?>? source)
    {
        if (source is not { Count: > 0 })
            return null;

        var values = new Dictionary<string, PxValue>(StringComparer.Ordinal);
        foreach (var (name, node) in source)
            values[name] = PxValueJson.FromJson(node);
        return values;
    }

    /// <summary>Синхронная уборка состояния (ранние отказы Start — запуск не состоялся).</summary>
    private static void DisposeState(object? state)
    {
        try
        {
            switch (state)
            {
                case IAsyncDisposable asyncDisposable:
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
        catch
        {
            // Ошибка уборки не должна заслонять причину отказа.
        }
    }

    private static async ValueTask DisposeStateAsync(object? state)
    {
        try
        {
            switch (state)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
        catch
        {
            // Ошибка уборки не должна заслонять итог исполнения.
        }
    }
}
