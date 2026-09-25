using Mars.Forms.Contracts;

namespace Mars.Cms.Contracts.MetaFields;

/// <summary>
/// Тип метаполя → тип поля общей формы. Коды обоих enum'ов совпадают (кроме имени
/// <c>Query</c>/<c>Computed</c>), поэтому маппинг — прямое приведение; совпадение кодов
/// стережёт тест <c>MetaFieldTypeFormMappingTests</c>. Один источник для сервера (дескриптор
/// поля в <c>MetaFieldFormMapping</c>) и клиента (определение поля в общем редакторе).
/// </summary>
public static class MetaFieldTypeFormMapping
{
    public static FormFieldType ToFormFieldType(this MetaFieldType type) => (FormFieldType)(int)type;
}
