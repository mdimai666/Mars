namespace Mars.Admin.Framework.Services;

/// <summary>
/// Боковая панель редактирования поста (детские объекты в секциях «Список объектов»).
/// Реализация-дровер монтируется в админке и регистрирует <see cref="ChildPostEditorService.Opener"/>.
/// </summary>
public interface IChildPostEditor
{
    /// <summary>
    /// Открыть редактор поста в боковой панели.
    /// </summary>
    /// <param name="postId"><see cref="Guid.Empty"/> — создание нового</param>
    /// <param name="postTypeName">имя типа поста</param>
    /// <param name="onSaved">вызывается после сохранения с Id поста</param>
    void Open(Guid postId, string postTypeName, Action<Guid>? onSaved = null);
}

public class ChildPostEditorService : IChildPostEditor
{
    /// <summary>Делегат дровера (регистрируется компонентом админки)</summary>
    public static Action<Guid, string, Action<Guid>?>? Opener { get; set; }

    public void Open(Guid postId, string postTypeName, Action<Guid>? onSaved = null)
        => Opener?.Invoke(postId, postTypeName, onSaved);
}
