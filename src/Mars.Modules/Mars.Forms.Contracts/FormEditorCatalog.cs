using System.Text.Json.Nodes;

namespace Mars.Forms.Contracts;

/// <summary>
/// Ключи встроенных редакторов значения поля — общий каталог всех провайдеров (CMS, ноды,
/// внешние таблицы, виджеты). Схема ключей трёхчастная
/// (<c>&lt;происхождение&gt;.&lt;семейство&gt;.&lt;реализация&gt;</c>): встроенные <c>core.*</c>,
/// плагины <c>plugin.*</c>. Компоненты редакторов регистрируются в реестре фронта
/// (<c>IFormEditorLocator</c>), здесь — только ключи, их названия и параметры поля. Выбор
/// редактора поля хранится в его параметрах (<see cref="EditorOption"/>); пусто — встроенный
/// редактор типа.
/// </summary>
public static class FormEditorCatalog
{
    // ---------- Ввод простых значений ----------

    public const string Text = "core.input.text";
    public const string Multiline = "core.input.multiline";
    public const string Number = "core.input.number";
    public const string Bool = "core.input.bool";

    /// <summary>Дата (встроенный редактор полей DateTime)</summary>
    public const string Date = "core.input.date";

    /// <summary>Время; хранится на дате-заглушке — дата смысла не несёт</summary>
    public const string Time = "core.input.time";

    /// <summary>Дата и время на одной колонке</summary>
    public const string DateTime = "core.input.datetime";

    public const string Select = "core.input.select";

    /// <summary>Множественный выбор по вариантам поля (чекбоксы); в значении — список ключей вариантов</summary>
    public const string Choices = "core.input.choices";

    /// <summary>Редактор множественных значений простых типов (строки, числа, даты, варианты)</summary>
    public const string List = "core.input.list";

    /// <summary>Цвет в <c>#rrggbb</c></summary>
    public const string Color = "core.input.color";

    public const string Url = "core.input.url";
    public const string Email = "core.input.email";

    /// <summary>Множество строк (теги, метки)</summary>
    public const string Tags = "core.input.tags";

    // ---------- Тяжёлые редакторы текста ----------

    public const string Wysiwyg = "core.wysiwyg.quilljs";

    /// <summary>Редактор кода; язык — <see cref="CodeLangOption"/></summary>
    public const string Code = "core.code.monaco";

    public const string BlockEditor = "core.blockeditor.editorjs";

    /// <summary>Язык кода по умолчанию для редактора <see cref="Code"/></summary>
    public const string DefaultCodeLang = "handlebars";

    // ---------- Отображение ----------

    /// <summary>Только чтение: значение строкой</summary>
    public const string TextDisplay = "core.display.text";

    /// <summary>Ключ параметра «редактор значения» в параметрах поля</summary>
    public static string EditorOption() => "editor";

    /// <summary>Ключ параметра «язык кода» в параметрах поля</summary>
    public static string CodeLangOption() => "codeLang";

    /// <summary>Редактор значения из параметров поля (пусто = встроенный редактор типа)</summary>
    public static string GetEditor(this JsonNode? options)
        => options is JsonObject obj && obj[EditorOption()] is JsonValue value && value.TryGetValue<string>(out var editor)
            ? editor
            : "";

    /// <summary>Язык кода из параметров поля (пусто = <see cref="DefaultCodeLang"/>)</summary>
    public static string GetCodeLang(this JsonNode? options)
        => options is JsonObject obj && obj[CodeLangOption()] is JsonValue value
            && value.TryGetValue<string>(out var lang) && lang.Length > 0
            ? lang
            : DefaultCodeLang;
}
