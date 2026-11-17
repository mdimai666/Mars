using Mars.AiChat.Contracts.Options;
using Mars.SiteEngine.Abstractions.Constants.Website;

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
        if (IsAdminPagesContext(pageContext, frontEditorSlug)) skills.Add("mars-admin-pages");
        if (IsPostEditPage(pageContext)) skills.Add("mars-posts");
        if (IsMediaPage(pageContext)) skills.Add("mars-media");
        if (IsDatasourcePage(pageContext)) skills.Add("mars-datasource");
        if (IsPrivacyPolicySettingsPage(pageContext)) skills.Add("mars-privacy-policy");
        if (option.EnableSqlAccess) skills.Add("mars-sql");

        return skills;
    }

    // Страницы админ-фронта и меню админки: /p/{path} (catch-all AdminFrontPage),
    // редактор фронта admin, /NavMenu и /EditNavMenu/{id}
    static bool IsAdminPagesContext(string? pageContext, string? frontEditorSlug)
    {
        if (string.Equals(frontEditorSlug, AppAdminConstants.AdminFrontSlug, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(pageContext)) return false;

        var segments = pageContext.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length >= 2 && segments[0].Equals("p", StringComparison.OrdinalIgnoreCase))
            return true;

        return segments.Any(seg => seg.Equals("NavMenu", StringComparison.OrdinalIgnoreCase)
                                   || seg.Equals("EditNavMenu", StringComparison.OrdinalIgnoreCase));
    }

    // Раздел настроек «Политика»: /Settings/Option/{FullName с '+' вместо '.'}
    static bool IsPrivacyPolicySettingsPage(string? pageContext)
    {
        if (string.IsNullOrWhiteSpace(pageContext)) return false;

        return pageContext.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(seg => seg.Contains("PrivacyPolicyOption", StringComparison.OrdinalIgnoreCase));
    }

    // Страницы модуля DataSource: /tech/datasource/{slug?}, /tech/datasource/config
    static bool IsDatasourcePage(string? pageContext)
    {
        if (string.IsNullOrWhiteSpace(pageContext)) return false;

        return pageContext.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(seg => seg.Equals("datasource", StringComparison.OrdinalIgnoreCase));
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
