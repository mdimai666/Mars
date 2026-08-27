namespace Mars.Contracts.MetaFields;

public enum MetaFieldType : int
{
    String = TypeCode.String,
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

    /// <summary>
    /// Вычислимое поле: хранится только определение (в <see cref="MetaFieldDetailBase.Options"/>),
    /// значение резолвится батчем на чтении
    /// </summary>
    Query = 110,
}
