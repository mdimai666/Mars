namespace Mars.Cms.Abstractions.Services;

/// <summary>
/// Модели-владельцы мета-значений (ключи провайдеров значений,
/// например <c>IMetaValueUniquenessProvider</c>). По маркеру поля
/// (meta_field_id) значение уже привязано к типу владельца
/// </summary>
public static class MetaValueOwnerCatalog
{
    public const string Post = "Post";
    public const string PostCategory = "PostCategory";
    public const string User = "User";
}
