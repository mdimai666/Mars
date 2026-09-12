namespace Mars.Forms.Front;

/// <summary>
/// Хуки фиксации значений формы: редакторы с отложенной записью (WYSIWYG, код, блочный редактор,
/// контент) регистрируются здесь, а страница забирает их значения одним вызовом перед сохранением.
/// Живёт в контексте рендера, поэтому один экземпляр обслуживает все зоны формы.
/// </summary>
public sealed class FormCommitHooks
{
    readonly List<Func<Task>> _hooks = [];

    public void Register(Func<Task> commit)
    {
        if (!_hooks.Contains(commit)) _hooks.Add(commit);
    }

    public void Unregister(Func<Task> commit) => _hooks.Remove(commit);

    /// <summary>Забрать значения всех зарегистрированных редакторов в модель</summary>
    public async Task CommitAllAsync()
    {
        foreach (var hook in _hooks.ToArray())
            await hook();
    }
}
