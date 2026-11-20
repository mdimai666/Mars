namespace Mars.Cms.Contracts.PostCategories;

public static class PostCategoryKpiKeys
{
    /// <summary>Все категории (глобально)</summary>
    public const string Total = "postcategories.total";

    /// <summary>Категории конкретного типа записей: "postcategories.total:{postTypeName}"</summary>
    public static string TotalFor(string postTypeName) => $"{Total}:{postTypeName}";
}
