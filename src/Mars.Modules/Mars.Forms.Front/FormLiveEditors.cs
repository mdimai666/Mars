namespace Mars.Forms.Front;

/// <summary>
/// Живые редакторы формы: редактор, который держит значение у себя (WYSIWYG, код, блочный),
/// регистрируется по ключу поля, чтобы значение можно было записать в него извне — инструментом
/// ИИ-агента или кнопкой сохранения самого редактора. Живёт в контексте рендера, поэтому один
/// экземпляр обслуживает все зоны формы.
/// </summary>
public sealed class FormLiveEditors
{
    readonly Dictionary<string, Func<string, Task<string?>>> _setters = new(StringComparer.Ordinal);

    /// <summary>Запрос сохранения формы (Ctrl+S в редакторе кода); null — сохранять некуда</summary>
    public Func<Task>? SaveRequest { get; set; }

    /// <summary>Регистрация редактора поля: <c>setValue</c> пишет значение, возвращает текст ошибки или null</summary>
    public void Register(string fieldKey, Func<string, Task<string?>> setValue)
        => _setters[fieldKey] = setValue;

    public void Unregister(string fieldKey, Func<string, Task<string?>> setValue)
    {
        if (_setters.TryGetValue(fieldKey, out var registered) && registered == setValue)
            _setters.Remove(fieldKey);
    }

    /// <summary>Запись значения в живой редактор поля; null — значение правит обычный редактор</summary>
    public Func<string, Task<string?>>? Find(string fieldKey) => _setters.GetValueOrDefault(fieldKey);
}
