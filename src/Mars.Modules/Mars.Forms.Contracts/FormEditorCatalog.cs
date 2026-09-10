namespace Mars.Forms.Contracts;

/// <summary>
/// Ключи встроенных редакторов общего слоя. Convention трёхчастных ключей тот же, что у
/// редакторов мета-полей (<c>&lt;происхождение&gt;.&lt;семейство&gt;.&lt;реализация&gt;</c>):
/// провайдеры и плагины добавляют свои ключи, тяжёлые редакторы регистрируются на фронте.
/// Обычный текст без ключа — дефолтный редактор типа.
/// </summary>
public static class FormEditorCatalog
{
    public const string Text = "core.input.text";
    public const string Multiline = "core.input.multiline";
    public const string Number = "core.input.number";
    public const string Bool = "core.input.bool";
    public const string Date = "core.input.date";
    public const string Select = "core.input.select";

    /// <summary>Множественный выбор по вариантам поля (чекбоксы); в значении — список ключей вариантов</summary>
    public const string Choices = "core.input.choices";

    /// <summary>Редактор множественных значений простых типов (строки, числа, даты, варианты)</summary>
    public const string List = "core.input.list";

    public static readonly IReadOnlyList<(string Key, string Title)> All =
    [
        (Text, "Текст"),
        (Multiline, "Многострочный текст"),
        (Number, "Число"),
        (Bool, "Да/Нет"),
        (Date, "Дата"),
        (Select, "Выбор из списка"),
        (Choices, "Выбор нескольких из списка"),
        (List, "Список значений"),
    ];
}
