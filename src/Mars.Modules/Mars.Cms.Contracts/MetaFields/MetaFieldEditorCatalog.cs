using System.Text.Json.Nodes;

namespace Mars.Cms.Contracts.MetaFields;

/// <summary>
/// Каталог ключей редакторов значений полей (общий для сервера и админки). Выбор редактора поля
/// хранится в <c>Options.editor</c>; пусто = дефолтный редактор типа. Схема ключей:
/// <c>&lt;происхождение&gt;.&lt;семейство&gt;.&lt;реализация&gt;</c> — встроенные редакторы <c>core.*</c>,
/// плагины <c>plugin.*</c>. Компоненты всех редакторов (и метаполей, и системных слотов) — в общем
/// реестре фронта (<c>IFormEditorLocator</c>); здесь остаются только ключи.
/// </summary>
public static class MetaFieldEditorCatalog
{
    // ---------- Простые редакторы ввода ----------

    /// <summary>Выбор цвета (для полей String)</summary>
    public const string Color = "core.input.color";

    /// <summary>URL-адрес (для полей String)</summary>
    public const string Url = "core.input.url";

    /// <summary>Email (для полей String)</summary>
    public const string Email = "core.input.email";

    /// <summary>Дата (для полей DateTime)</summary>
    public const string Date = "core.input.date";

    /// <summary>Время (для полей DateTime)</summary>
    public const string Time = "core.input.time";

    /// <summary>Дата и время (для полей DateTime)</summary>
    public const string DateTime = "core.input.datetime";

    // ---------- Тяжёлые редакторы текста ----------

    /// <summary>WYSIWYG — Quill (Blazored.TextEditor)</summary>
    public const string Wysiwyg = "core.wysiwyg.quilljs";

    /// <summary>Редактор кода — Monaco (CodeEditor2); язык — <c>Options.codeLang</c></summary>
    public const string Code = "core.code.monaco";

    /// <summary>Блочный редактор — Editor.js (BlockEditor1)</summary>
    public const string BlockEditor = "core.blockeditor.editorjs";

    /// <summary>Язык кода по умолчанию для редактора <see cref="Code"/></summary>
    public const string DefaultCodeLang = "handlebars";

    /// <summary>Ключ опции редактора поля в Options</summary>
    public static string EditorOption() => "editor";

    /// <summary>Ключ опции языка кода в Options</summary>
    public static string CodeLangOption() => "codeLang";

    /// <summary>Редактор поля из <c>Options.editor</c> (пусто = дефолтный редактор типа)</summary>
    public static string GetEditor(this JsonNode? options)
        => options is JsonObject obj && obj[EditorOption()] is JsonValue value && value.TryGetValue<string>(out var editor)
            ? editor
            : "";

    /// <summary>Язык кода из <c>Options.codeLang</c> (пусто = <see cref="DefaultCodeLang"/>)</summary>
    public static string GetCodeLang(this JsonNode? options)
        => options is JsonObject obj && obj[CodeLangOption()] is JsonValue value
            && value.TryGetValue<string>(out var lang) && lang.Length > 0
            ? lang
            : DefaultCodeLang;
}
