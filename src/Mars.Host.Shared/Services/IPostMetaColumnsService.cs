namespace Mars.Host.Shared.Services;

/// <summary>
/// Батч-получение отображаемых значений мета-полей для колонок грида списка постов.
/// </summary>
public interface IPostMetaColumnsService
{
    /// <summary>
    /// Возвращает postId → (ключ поля → отображаемое значение). Скаляры форматируются,
    /// Select/SelectMany — заголовки вариантов, Relation/File/Image — заголовки целей.
    /// Поля <c>Query</c> и неизвестные ключи пропускаются.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string?>>> GetDisplayValuesAsync(
        string typeName,
        IReadOnlyCollection<string> fieldKeys,
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken = default);
}
