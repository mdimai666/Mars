using System.Text.Json.Nodes;

namespace Mars.Contracts.MetaFields;

/// <summary>
/// Каталог маркеров «вида» мета-полей (общий для сервера и админки).
/// Вид хранится в <c>Options.kind</c> и определяет поведение поля в админке:
/// обычный рендер по типу поля или специализированный (список объектов и т.п.).
/// </summary>
public static class MetaFieldKindCatalog
{
    /// <summary>
    /// Упорядоченный список объектов: мульти-значения Relation-поля на стороне родителя,
    /// рендерятся секцией детей (таблица + боковая панель редактирования).
    /// </summary>
    public const string List = "list";

    /// <summary>Поведение при удалении значения из списка</summary>
    public static class RemoveModes
    {
        /// <summary>Отвязать от родителя (пост-цель не удаляется)</summary>
        public const string Unlink = "unlink";

        /// <summary>Удалить пост-цель с подтверждением</summary>
        public const string DeleteConfirm = "delete-confirm";
    }

    /// <summary>Ключ опции вида поля в Options</summary>
    public static string KindOption() => "kind";

    /// <summary>Ключ опции режима удаления в Options</summary>
    public static string RemoveModeOption() => "removeMode";

    /// <summary>Ключ опции папки загрузки (поля Файл/Изображение; пусто = папка года)</summary>
    public static string UploadFolderOption() => "uploadFolder";

    /// <summary>Ключ опции видимости дроп-зоны в редакторах значений (отсутствие = включена)</summary>
    public static string DropZoneOption() => "dropZone";

    /// <summary>Ключ опции вида списка детей в Options</summary>
    public static string ViewModeOption() => "viewMode";

    /// <summary>Вид отображения списка объектов</summary>
    public static class ViewModes
    {
        /// <summary>Таблица (дефолт)</summary>
        public const string Table = "table";

        /// <summary>Карточки с превью</summary>
        public const string Cards = "cards";
    }

    /// <summary>Вид поля из <c>Options.kind</c> (пусто = обычный)</summary>
    public static string GetKind(this JsonNode? options)
        => options is JsonObject obj && obj[KindOption()] is JsonValue value && value.TryGetValue<string>(out var kind)
            ? kind
            : "";

    /// <summary>Поле — упорядоченный список объектов</summary>
    public static bool IsListKind(this JsonNode? options)
        => options.GetKind() == List;

    /// <summary>Режим удаления значений из <c>Options.removeMode</c> (пусто = по видимости типа-цели)</summary>
    public static string GetRemoveMode(this JsonNode? options)
        => options is JsonObject obj && obj[RemoveModeOption()] is JsonValue value && value.TryGetValue<string>(out var mode)
            ? mode
            : "";

    /// <summary>Папка загрузки из <c>Options.uploadFolder</c> (пусто = папка года)</summary>
    public static string GetUploadFolder(this JsonNode? options)
        => options is JsonObject obj && obj[UploadFolderOption()] is JsonValue value && value.TryGetValue<string>(out var folder)
            ? folder
            : "";

    /// <summary>Дроп-зона в редакторах значений включена (отсутствие ключа = включена)</summary>
    public static bool IsDropZoneEnabled(this JsonNode? options)
        => !(options is JsonObject obj
             && obj[DropZoneOption()] is JsonValue value
             && value.TryGetValue<bool>(out var enabled)
             && !enabled);

    /// <summary>Вид отображения списка детей из <c>Options.viewMode</c> (пусто = таблица)</summary>
    public static string GetViewMode(this JsonNode? options)
        => options is JsonObject obj && obj[ViewModeOption()] is JsonValue value && value.TryGetValue<string>(out var mode)
            ? mode
            : "";
}
