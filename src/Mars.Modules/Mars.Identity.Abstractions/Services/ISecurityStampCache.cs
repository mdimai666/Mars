namespace Mars.Identity.Abstractions.Services;

/// <summary>
/// In-memory отметки «security stamp пользователя обновлён». Валидация cookie сверяет
/// stamp из куки с отметкой на каждом запросе (дешёво, без БД) и перевыпускает principal
/// сразу, не дожидаясь SecurityStampValidatorOptions.ValidationInterval.
/// Промахи кэша (рестарт, другой инстанс) покрывает штатная периодическая сверка с БД.
/// </summary>
public interface ISecurityStampCache
{
    void Mark(Guid userId, string newSecurityStamp);
    bool TryGetNewStamp(Guid userId, out string newSecurityStamp);
}
