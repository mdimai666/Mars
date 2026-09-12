namespace Mars.Forms.Contracts;

/// <summary>
/// Правила раскладки формы: какие узлы лежат в каких. Одна матрица на всех потребителей —
/// нормализатор проверяет ею сохранённую раскладку, дизайнер — допустимость переноса узла.
/// </summary>
public static class FormLayoutRules
{
    /// <summary>
    /// Допустим ли узел ребёнком узла такого типа: <paramref name="parent"/> = null — корень зоны.
    /// Элемент (поле, заголовок, разделитель) живёт только в колонке: в ряду и в зоне ему места нет.
    /// </summary>
    public static bool CanContain(FormItemKind? parent, FormItemKind child) => parent switch
    {
        null => child is FormItemKind.Container or FormItemKind.Row,

        FormItemKind.Container => child == FormItemKind.Row,
        FormItemKind.Row => child == FormItemKind.Column,
        FormItemKind.Column => IsElement(child) || child == FormItemKind.Row,

        _ => false,
    };

    /// <summary>Листовой элемент раскладки: поле, заголовок или разделитель — только внутри колонки</summary>
    public static bool IsElement(FormItemKind kind)
        => kind is FormItemKind.Field or FormItemKind.Heading or FormItemKind.Divider;

    /// <summary>Известен ли такой тип узла раскладки</summary>
    public static bool IsKnown(FormItemKind kind)
        => IsElement(kind) || kind is FormItemKind.Container or FormItemKind.Row or FormItemKind.Column;
}
