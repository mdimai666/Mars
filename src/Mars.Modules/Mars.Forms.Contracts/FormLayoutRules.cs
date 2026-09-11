namespace Mars.Forms.Contracts;

/// <summary>
/// Правила раскладки формы: какие узлы лежат в каких. Одна матрица на всех потребителей —
/// нормализатор проверяет ею сохранённую раскладку, дизайнер — допустимость переноса узла.
/// </summary>
public static class FormLayoutRules
{
    /// <summary>
    /// Допустим ли узел ребёнком узла такого типа: <paramref name="parent"/> = null — корень зоны.
    /// Поле, заголовок и разделитель — листовые узлы, контейнерами быть не могут.
    /// </summary>
    public static bool CanContain(FormItemKind? parent, FormItemKind child) => parent switch
    {
        null => child is FormItemKind.Container or FormItemKind.Row or FormItemKind.Field
                     or FormItemKind.Heading or FormItemKind.Divider,

        FormItemKind.Container => child is FormItemKind.Row or FormItemKind.Field
                                       or FormItemKind.Heading or FormItemKind.Divider,

        FormItemKind.Row => child is FormItemKind.Column or FormItemKind.Field
                                 or FormItemKind.Heading or FormItemKind.Divider,

        FormItemKind.Column => child is FormItemKind.Row or FormItemKind.Field
                                    or FormItemKind.Heading or FormItemKind.Divider,

        _ => false,
    };

    /// <summary>Известен ли такой тип узла раскладки</summary>
    public static bool IsKnown(FormItemKind kind)
        => kind is FormItemKind.Field or FormItemKind.Container or FormItemKind.Row
                 or FormItemKind.Column or FormItemKind.Heading or FormItemKind.Divider;
}
