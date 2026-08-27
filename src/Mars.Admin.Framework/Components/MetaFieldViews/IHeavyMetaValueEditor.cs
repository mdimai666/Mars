namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// «Тяжёлый» редактор значения: пока пользователь печатает, значение в модель
/// не пушится — форма сама забирает его перед сохранением (<see cref="CommitAsync"/>).
/// <see cref="GetValueAsync"/>/<see cref="SetValueAsync"/> — программный доступ
/// к живому значению (ИИ-мост и т.п.).
/// Лёгкие инлайн-редакторы остаются реактивными (ValueChanged на каждое изменение).
/// </summary>
public interface IHeavyMetaValueEditor
{
    /// <summary>Текущее значение в редакторе (может отличаться от модели, пока печатают)</summary>
    Task<string?> GetValueAsync();

    /// <summary>Записать значение в редактор программно</summary>
    Task SetValueAsync(string? value);

    /// <summary>Забрать значение из редактора в модель (вызывает форма перед сохранением)</summary>
    Task CommitAsync();
}
