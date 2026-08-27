using FluentValidation;

namespace Mars.Host.Shared.Dto.Users;

public class UsernameBlacklistValidator : AbstractValidator<string>
{
    private static readonly HashSet<string> Blacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        // системные / служебные
        "admin", "administrator", "root", "system", "superuser", "sys", "sysadmin", "security", "guest",
        "null", "undefined", "unknown", "username", "login", "account", "owner",
        "server", "mail", "email", "api", "bot", "backend", "frontend", "service", "services",

        // маршруты / URL
        "auth", "signin", "signup", "register", "logout", "account", "profile",
        "settings", "dashboard", "home", "index", "about", "contact", "help",
        "docs", "terms", "privacy", "search", "static", "assets", "uploads", "upload",
        "images", "files", "favicon", "robots", "sitemap", "status", "health",

        // пошлые / оскорбительные (цензурировано)
        "sex", "porn", "xxx", "boobs", "dick",
        "nazi", "terror", "suicide", "kill", "murder", "slave", "rapist", "pedo",
        "whore", "slut", "gaysex", "lesbian", "drug", "weed", "cocaine", "heroin",
        "racist", "tranny", "spammer", "hack", "cheater", "faker",

        // социально неприемлемые
        "islamist", "terrorist", "hitler", "covid", "corona", "war",
        "god", "jesus", "allah", "satan", "lucifer", "devil",

        // дубли для частых обходов
        "adm1n", "adm!n", "adm-in", "superuser1", "mod",
    };

    public UsernameBlacklistValidator()
    {
        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(v => !IsBlacklisted(v))
                .WithMessage("This username is reserved or not allowed.");
    }

    public static bool IsBlacklisted(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = value.Trim().ToLowerInvariant();

        if (Blacklist.Contains(normalized))
            return true;

        return false;
    }
}
