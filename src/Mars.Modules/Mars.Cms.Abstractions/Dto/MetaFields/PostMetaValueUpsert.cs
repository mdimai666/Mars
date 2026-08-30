namespace Mars.Cms.Abstractions.Dto.MetaFields;

/// <summary>
/// Точечная запись значения мета-поля поста: существующая строка значения
/// обновляется, при отсутствии — создаётся. Используется перегенерацией значений.
/// </summary>
public record PostMetaValueUpsert(Guid PostId, MetaFieldDto MetaField, object Value);
