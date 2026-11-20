namespace Mars.Cms.Contracts.Posts;

public static class PostKpiKeys
{
    /// <summary>Все записи (глобально)</summary>
    public const string Total = "posts.total";

    /// <summary>Записи конкретного типа: "posts.total:{postTypeName}"</summary>
    public static string TotalFor(string postTypeName) => $"{Total}:{postTypeName}";
}
