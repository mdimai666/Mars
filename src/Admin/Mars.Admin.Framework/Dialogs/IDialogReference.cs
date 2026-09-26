using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Dialogs;

/// <summary>
/// v4-совместимая ссылка на показанный диалог: <see cref="Result"/> завершается при закрытии.
/// </summary>
public interface IDialogReference
{
    Task<DialogResult> Result { get; }
}

internal sealed class DialogReference(Task<DialogResult> result) : IDialogReference
{
    public Task<DialogResult> Result { get; } = result;
}
