namespace Mars.Forms.Contracts;

/// <summary>
/// Тип поля формы — общий для всех провайдеров (посты, ноды, внешние таблицы, виджеты).
/// Коды совпадают с <c>MetaFieldType</c>/<c>EMetaFieldType</c> (CMS) и с <see cref="TypeCode"/>
/// для примитивов, поэтому маппинг источника — прямое приведение, а не таблица соответствий.
/// Значения зафиксированы: на провод enum уходит числом.
/// </summary>
public enum FormFieldType : int
{
    /// <summary>Короткая строка (255)</summary>
    String = TypeCode.String,

    /// <summary>Длинный текст</summary>
    Text = 28,

    Bool = TypeCode.Boolean,
    Int = TypeCode.Int32,
    Long = TypeCode.Int64,
    Float = TypeCode.Single,
    Decimal = TypeCode.Decimal,
    DateTime = TypeCode.DateTime,

    Select = 30,
    SelectMany = 31,

    Relation = 100,
    File = 101,
    Image = 102,

    /// <summary>Составное значение произвольной формы (свойства нод, json-колонки)</summary>
    Object = 40,

    /// <summary>Вычислимое поле (в CMS — <c>MetaFieldType.Query</c>): значение не передаётся, резолвится на чтении</summary>
    Computed = 110,
}
