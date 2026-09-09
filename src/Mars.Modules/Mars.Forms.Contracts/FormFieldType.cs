namespace Mars.Forms.Contracts;

/// <summary>
/// Тип поля формы — общий для всех провайдеров (посты, ноды, внешние таблицы, виджеты).
/// Значения зафиксированы: на провод enum уходит числом.
/// Маппинги источников: <c>MetaFieldType</c> (CMS), CLR-тип <c>QTableColumn.DataType</c>
/// (внешние базы), <c>XActionArgument.Type</c>.
/// </summary>
public enum FormFieldType
{
    String = 1,
    Text = 2,
    Bool = 3,
    Int = 4,
    Long = 5,
    Float = 6,
    Decimal = 7,
    DateTime = 8,

    Select = 10,
    SelectMany = 11,

    Relation = 20,
    File = 21,
    Image = 22,

    /// <summary>Составное значение произвольной формы (свойства нод, json-колонки)</summary>
    Object = 30,

    /// <summary>Вычислимое поле: значение не передаётся, резолвится на чтении</summary>
    Computed = 40,
}
