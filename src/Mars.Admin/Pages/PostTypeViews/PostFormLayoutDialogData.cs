namespace Mars.Admin.Pages.PostTypeViews;

/// <summary>Данные диалога быстрой правки раскладки формы типа (вход из формы поста)</summary>
public class PostFormLayoutDialogData
{
    public required Guid PostTypeId { get; init; }

    /// <summary>Вызывается после сохранения раскладки — хост подменяет своё дерево формы</summary>
    public Func<Task>? OnSaved { get; init; }
}
