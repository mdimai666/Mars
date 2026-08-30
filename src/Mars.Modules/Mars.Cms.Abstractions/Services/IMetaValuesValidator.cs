using System.Text.Json.Nodes;
using Mars.Cms.Abstractions.Dto.MetaFields;

namespace Mars.Cms.Abstractions.Services;

/// <summary>
/// Проверка значений мета-полей по определению поля: обязательность (IsNullable),
/// диапазон (Min/Max), правила из Options.validators. Общий для всех владельцев
/// (посты, пользователи, категории) и для JSON-пути записи.
/// </summary>
public interface IMetaValuesValidator
{
    /// <summary>Ошибки значений (пусто — валидно); ключ ошибки — Key поля.
    /// Контекст — владелец значений (вид и ид сохраняемого объекта)</summary>
    Task<IReadOnlyCollection<MetaValueValidationError>> ValidateAsync(IReadOnlyCollection<ModifyMetaValueDetailQuery> values,
                                                                      MetaValueValidationContext context,
                                                                      CancellationToken cancellationToken = default);

    /// <summary>Ошибки значений из JSON-записи (ключи словаря — Key полей).
    /// requireAll — проверять обязательность и для отсутствующих ключей (создание);
    /// при обновлении meta приходит частично.
    /// contentFieldKey — поле контента фичи: значение хранится в posts.Content
    /// и в мета-значениях не участвует, из проверки исключается.</summary>
    Task<IReadOnlyCollection<MetaValueValidationError>> ValidateJsonAsync(IReadOnlyCollection<MetaFieldDto> fields,
                                                                          IReadOnlyDictionary<string, JsonNode>? meta,
                                                                          bool requireAll,
                                                                          MetaValueValidationContext context,
                                                                          string? contentFieldKey = null,
                                                                          CancellationToken cancellationToken = default);
}

public record MetaValueValidationError(string FieldKey, string Message);
