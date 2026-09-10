namespace Mars.Admin.Pages.PostTypeViews;

/// <summary>Данные диалога выбора поля картинки поста при включении фичи</summary>
public class PostImageSelectDialogData
{
    /// <summary>Существующие поля-изображения типа (ключ + заголовок)</summary>
    public required IReadOnlyCollection<(string Key, string Title)> Options { get; init; }
}
