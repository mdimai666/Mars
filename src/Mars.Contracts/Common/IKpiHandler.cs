namespace Mars.Contracts.Common;

public interface IKpiHandler
{
    string Key { get; }
    Task<KpiResult> GetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Метрика с параметром в ключе запроса: "{Key}:{parameter}" (например "posts.total:blog" —
/// число записей конкретного типа). Контроллер при запросе ключа с ':' после первого двоеточия
/// вызывает <see cref="GetAsync(string, CancellationToken)"/>; запрос по чистому <see cref="IKpiHandler.Key"/>
/// идёт через базовый метод (глобальное значение). KpiResult.Key результата — ПОЛНЫЙ запрошенный ключ,
/// Label — стабильный ключ без параметра (переводится IStringLocalizer).
/// </summary>
public interface IParameterizedKpiHandler : IKpiHandler
{
    Task<KpiResult> GetAsync(string parameter, CancellationToken cancellationToken);
}
