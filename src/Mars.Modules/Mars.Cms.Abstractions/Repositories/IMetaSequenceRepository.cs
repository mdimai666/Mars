namespace Mars.Cms.Abstractions.Repositories;

/// <summary>
/// Счётчики генератора «порядковый номер» (таблица meta_sequences):
/// на пару (поле, скоуп) — последнее выданное значение.
/// </summary>
public interface IMetaSequenceRepository : IDisposable
{
    /// <summary>Атомарно выдаёт следующее значение счётчика (инкремент с защитой от гонок).</summary>
    Task<long> NextValueAsync(Guid metaFieldId, string scopeKey, CancellationToken cancellationToken);

    /// <summary>Устанавливает счётчик в заданное значение (создаёт строку при отсутствии) — фиксация после перенумерации.</summary>
    Task SetValueAsync(Guid metaFieldId, string scopeKey, long value, CancellationToken cancellationToken);
}
