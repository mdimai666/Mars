namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Реестр тяжёлых редакторов значений (WYSIWYG, код, блочный): значение забирается в модель
/// при сохранении формы, а не пушится при вводе. Отдан каскадом, чтобы страница могла раздать
/// один реестр на несколько зон формы (см. <c>ai/FormEnginePlan.md</c>, фаза 1).
/// </summary>
public interface IHeavyMetaValueEditors
{
    void RegisterHeavyEditor(IHeavyMetaValueEditor editor);

    void UnregisterHeavyEditor(IHeavyMetaValueEditor editor);

    /// <summary>Забрать значения из всех тяжёлых редакторов в модель — вызывать перед сохранением</summary>
    Task PullAsync();
}

public class HeavyMetaValueEditorRegistry : IHeavyMetaValueEditors
{
    readonly List<IHeavyMetaValueEditor> _editors = [];

    public void RegisterHeavyEditor(IHeavyMetaValueEditor editor)
    {
        if (!_editors.Contains(editor)) _editors.Add(editor);
    }

    public void UnregisterHeavyEditor(IHeavyMetaValueEditor editor) => _editors.Remove(editor);

    public async Task PullAsync()
    {
        foreach (var editor in _editors.ToArray())
            await editor.CommitAsync();
    }
}
