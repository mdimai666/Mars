using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Mars.Datasource.Contracts.Models;

public class DatasourceConfig
{

    [Display(Name = "Название")]
    public string Title { get; set; } = "";

    [Display(Name = "Slug")]
    [Required]
    public string Slug { get; set; } = "";

    [Display(Name = "ConnectionString")]
    [Required]
    public string ConnectionString { get; set; } = "";

    [Display(Name = "Driver")]
    public string Driver { get; set; } = "psql";

    [Display(Name = "Disabled")]
    public bool Disabled { get; set; }

    /// <summary>Slug основной базы Mars — зарезервирован, в опции его задать нельзя.</summary>
    public const string DefaultSlug = "default";

    public string Label => string.IsNullOrEmpty(Title) ? Slug : Title;

    /// <summary>
    /// Правила slug. Возвращает текст ошибки или null. Дубли по slug проверяет тот, у кого есть весь список.
    /// </summary>
    public static string? ValidateSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "Slug не может быть пустым";
        if (slug.Equals(DefaultSlug, StringComparison.OrdinalIgnoreCase)) return $"Slug \"{DefaultSlug}\" зарезервирован";
        if (slug.Length is < 2 or > 32) return "Slug: от 2 до 32 символов";
        if (!Regex.IsMatch(slug, "^[a-z0-9][a-z0-9_-]*$")) return "Slug: строчные латинские буквы, цифры, дефис и подчёркивание";

        return null;
    }

    Dictionary<string, string> ConnStringParts()
    {
        Dictionary<string, string> parts = new(StringComparer.InvariantCultureIgnoreCase);

        foreach (var pair in ConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2) continue;

            parts[kv[0].Trim()] = kv[1].Trim();
        }

        return parts;
    }

    public string GetDatabaseName()
        => ConnStringParts().TryGetValue("database", out var name) ? name : "";

    public bool IsDefaultString()
    {
        return GetDatabaseName() == "database";
    }
}
