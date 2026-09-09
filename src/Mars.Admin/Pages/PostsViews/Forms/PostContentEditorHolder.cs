namespace Mars.Admin.Pages.PostsViews.Forms;

/// <summary>
/// Держатель живого редактора контента: контент рендерится внутри дерева формы (в любой зоне),
/// а доступ к нему нужен странице — для сохранения и для инструментов ИИ-агента.
/// </summary>
public sealed class PostContentEditorHolder
{
    public PostContentEditor? Current { get; set; }

    /// <summary>Запрос сохранения формы (Ctrl+S в редакторе кода)</summary>
    public Func<Task>? RequestSave { get; set; }
}
