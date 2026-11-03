namespace Mars.Admin.Framework.Extensions;

/// <summary>
/// Семантические тинты (0..4) для CloudyTag. Единственный источник «цветных» оттенков:
/// тег без явного тинта — всегда primary. Значения соответствуют .cloudy-tint-N
/// (0 blue, 1 purple, 2 green, 3 orange, 4 teal).
/// </summary>
public static class CloudyTint
{
    /// <summary>Тинт статуса поста по slug; пользовательские статусы → null (primary).</summary>
    public static int? PostStatus(string? slug) => slug switch
    {
        "draft" => 3,
        "pending" => 1,
        "publish" => 2,
        "hidden" => 4,
        _ => slug?.StableTint(),
    };

    /// <summary>Тинт типа фидбека; имена значений FeedbackType (Mars.Cms.Abstractions).</summary>
    public static int? Feedback(string? type) => type switch
    {
        "InfoMessage" => 0,
        "BugReport" => 3,
        "Question" => 1,
        _ => type?.StableTint(),
    };

    public const int Disabled = 3;
    public const int ComponentTypes = 1;
}
