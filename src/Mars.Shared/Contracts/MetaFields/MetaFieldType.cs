namespace Mars.Shared.Contracts.MetaFields;

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

    Group = 40,
    List = 50,

    Relation = 100,
    File = 101,
    Image = 102,
}
