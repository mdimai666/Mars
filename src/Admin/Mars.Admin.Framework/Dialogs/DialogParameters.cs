namespace Mars.Admin.Framework.Dialogs;

/// <summary>
/// v4-совместимые параметры диалога; маппятся на <c>DialogOptions</c> FluentUI v5.
/// Свойства без маппинга принимаются, но игнорируются (в v5 поведение по умолчанию).
/// </summary>
public class DialogParameters
{
    public string? Title { get; set; }

    public string? Width { get; set; }

    public string? Height { get; set; }

    public bool Modal { get; set; } = true;

    /// <summary>Кнопка закрытия (X) в заголовке; null — не трогать настройку v5.</summary>
    public bool? ShowDismiss { get; set; }

    public bool PreventDismissOnOverlayClick { get; set; }

    public bool PreventScroll { get; set; }

    public bool TrapFocus { get; set; }
}
