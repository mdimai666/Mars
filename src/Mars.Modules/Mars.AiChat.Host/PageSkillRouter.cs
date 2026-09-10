using Mars.AiChat.Contracts.Options;

namespace Mars.AiChat.Host;

/// <summary>
/// Детерминированный роутинг «страница/контекст → скилл»: полные инструкции этих скиллов
/// preload-ятся в контекст запуска, чтобы агент сразу знал, какими инструментами работать
/// (например, пользователь на странице редактирования поста — подгружается mars-posts).
/// </summary>
internal static class PageSkillRouter
{
    public static IReadOnlyList<string> Route(string? pageContext, string? frontEditorSlug, AiChatOption option)
    {
        var skills = new List<string>();

        if (frontEditorSlug is not null) skills.Add("mars-front-editor");
        if (IsPostEditPage(pageContext)) skills.Add("mars-posts");
        if (IsMediaPage(pageContext)) skills.Add("mars-media");
        if (option.EnableSqlAccess) skills.Add("mars-sql");

        return skills;
    }

    // Страница «Медиа» админки: /Media
    static bool IsMediaPage(string? pageContext)
    {
        if (string.IsNullOrWhiteSpace(pageContext)) return false;

        return pageContext.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(seg => seg.Equals("Media", StringComparison.OrdinalIgnoreCase));
    }

    // Страница редактирования поста: /Post/{posttype} (сегмент "Post" целиком,
    // чтобы не матчить /PostType, /PostCategory и т.п.)
    static bool IsPostEditPage(string? pageContext)
    {
        if (string.IsNullOrWhiteSpace(pageContext)) return false;

        return pageContext.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(seg => seg.Equals("Post", StringComparison.OrdinalIgnoreCase));
    }
}
