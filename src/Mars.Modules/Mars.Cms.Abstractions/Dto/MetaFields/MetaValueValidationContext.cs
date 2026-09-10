using Mars.Cms.Abstractions.Services;

namespace Mars.Cms.Abstractions.Dto.MetaFields;

/// <summary>
/// Контекст проверки значений мета-полей: модель-владелец значений и его ид
/// (для правил, обращающихся к данным — <c>unique</c>).
/// Field и UniquenessProvider заполняются валидатором повалидно
/// </summary>
public sealed record MetaValueValidationContext
{
    /// <summary>Модель-владелец значений (ключ <see cref="MetaValueOwnerCatalog"/>);
    /// пусто — правила, требующие домен, не проверяются</summary>
    public string? ModelName { get; init; }

    /// <summary>Ид сохраняемого владельца (при создании — пусто)</summary>
    public Guid? OwnerId { get; init; }

    /// <summary>Проверяемое поле (заполняется валидатором)</summary>
    public MetaFieldDto? Field { get; init; }

    /// <summary>Провайдер уникальности домена (заполняется валидатором)</summary>
    public IMetaValueUniquenessProvider? UniquenessProvider { get; init; }
}
