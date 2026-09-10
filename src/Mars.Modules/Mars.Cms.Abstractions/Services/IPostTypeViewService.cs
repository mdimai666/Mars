namespace Mars.Cms.Abstractions.Services;

/// <summary>
/// Генерируемые SQL-представления (views) типов постов: разворот мета-значений в колонки.
/// Артефакт генерации по требованию, не данные; инвалидируется при изменении типа.
/// </summary>
public interface IPostTypeViewService
{
    /// <summary>
    /// Создаёт или обновляет представление типа; возвращает имя представления
    /// </summary>
    Task<string> EnsureViewAsync(string typeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет представление типа (если существует)
    /// </summary>
    Task DropViewAsync(string typeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Читает представление с выборочными колонками (column pruning); колонки маппятся
    /// на свойства <typeparamref name="T"/> по имени
    /// </summary>
    /// <param name="properties">имена нужных свойств; null — все колонки</param>
    /// <param name="take">ограничение числа строк</param>
    Task<IReadOnlyList<T>> ListFromViewAsync<T>(string typeName,
                                                IEnumerable<string>? properties = null,
                                                int? take = null,
                                                CancellationToken cancellationToken = default) where T : new();
}
