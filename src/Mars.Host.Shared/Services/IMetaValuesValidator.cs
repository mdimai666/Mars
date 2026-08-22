using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.MetaFields;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Проверка значений мета-полей по определению поля: обязательность (IsNullable),
/// диапазон (Min/Max), правила из Options.validators. Общий для всех владельцев
/// (посты, пользователи, категории) и для JSON-пути записи.
/// </summary>
public interface IMetaValuesValidator
{
    /// <summary>Ошибки значений (пусто — валидно); ключ ошибки — Key поля</summary>
    IReadOnlyCollection<MetaValueValidationError> Validate(IReadOnlyCollection<ModifyMetaValueDetailQuery> values);

    /// <summary>Ошибки значений из JSON-записи (ключи словаря — Key полей).
    /// requireAll — проверять обязательность и для отсутствующих ключей (создание);
    /// при обновлении meta приходит частично.</summary>
    IReadOnlyCollection<MetaValueValidationError> ValidateJson(IReadOnlyCollection<MetaFieldDto> fields,
                                                               IReadOnlyDictionary<string, JsonNode>? meta,
                                                               bool requireAll);
}

public record MetaValueValidationError(string FieldKey, string Message);
