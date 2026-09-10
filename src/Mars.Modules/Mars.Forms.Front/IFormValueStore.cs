using Mars.Forms.Contracts;

namespace Mars.Forms.Front;

/// <summary>
/// Хранилище значений формы: типизированный доступ по дескриптору поля. Реализации —
/// JSON-мешок (<see cref="FormValuesModel"/>) и носитель владельца (например EAV-строки
/// мета-значений): рендерер и редакторы работают только с этим контрактом, поэтому значения
/// могут жить и в мешке, и в типизированном транспорте, и в таблице.
/// </summary>
public interface IFormValueStore
{
    /// <summary>Ошибки от провайдера — для подсветки полей</summary>
    IReadOnlyCollection<FormError> Errors { get; set; }

    /// <summary>Сигнал изменения значения (рендерер перерисовывается сам, это для внешних подписчиков)</summary>
    event Action? Changed;

    /// <summary>Значение поля в CLR-форме типа (<see cref="FormValueCodec"/>); для множественных — список</summary>
    object? GetValue(FormFieldDescriptor field);

    IReadOnlyList<object?> GetList(FormFieldDescriptor field);

    void SetValue(FormFieldDescriptor field, object? value);

    void SetList(FormFieldDescriptor field, IEnumerable<object?> values);

    /// <summary>
    /// Нативный носитель значения в хранилище источника (например строки мета-значений с их Id
    /// и признаком удаления) — для доменных редакторов, которым мало CLR-значения.
    /// null, когда у хранилища нет носителя (JSON-мешок).
    /// </summary>
    object? NativeValue(FormFieldDescriptor field);
}
