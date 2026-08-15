using Mars.PxBlocks.Host.Shared.Dto;

namespace Mars.PxBlocks.Host.Shared.Services;

/// <summary>REST-контракт api/PxBlocks со стороны клиента (редактора).</summary>
public interface IPxBlocksApiClient
{
    /// <summary>Определения блоков и toolbox сервера.</summary>
    Task<PxDefinitionsResponse> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Зарегистрированные контексты редакторов.</summary>
    Task<IReadOnlyList<PxEditorContextInfo>> GetContextsAsync(CancellationToken cancellationToken = default);

    /// <summary>Определения блоков и toolbox контекста.</summary>
    Task<PxDefinitionsResponse> GetContextDefinitionsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Запуск программы на сервере.</summary>
    Task<PxRunResponse> RunAsync(PxRunRequest request, CancellationToken cancellationToken = default);

    /// <summary>Остановка запуска. false — запуск серверу неизвестен.</summary>
    Task<bool> StopAsync(Guid runId, CancellationToken cancellationToken = default);
}
