using Mars.Admin.Framework.Components.MetaFieldViews;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Мета-контекст формы владельца: значения (EAV-строки) и определения полей. Одна типизированная
/// каскада вместо пары необработанных списков — её используют и форма поста, и формы пользователей
/// с категориями, поэтому мета-редакторы не знают, чья форма их отрисовала.
/// </summary>
public sealed class MetaValueContext
{
    public required List<MetaValueEditModel> Values { get; init; }

    public required List<MetaFieldEditModel> Fields { get; init; }
}
