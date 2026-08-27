using Mars.Contracts.XActions;

namespace Mars.WebApiClient.Interfaces;

public interface IActServiceClient
{
    Task<XActResult> Inject(string actionId, IReadOnlyDictionary<string, string>? args = null);

    /// <summary>
    /// Список команд для UI (без системных).
    /// </summary>
    Task<IReadOnlyDictionary<string, XActionCommand>> List();

    /// <summary>
    /// Динамические варианты выбора аргумента (запрашиваются перед отрисовкой формы).
    /// </summary>
    Task<IReadOnlyCollection<XActionOption>> Options(string sourceKey);
}
