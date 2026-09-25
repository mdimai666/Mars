namespace Mars.TemplateEngine.Providers.HandlebarsProvider;

/// <summary>
/// Области применения <see cref="IHandlebarsBuilderContributor"/>:
/// Core — движок шаблонов нод (Core.Handlebars), Site — движок рендера сайта.
/// </summary>
public static class HandlebarsScopes
{
    public const string Core = "core";
    public const string Site = "site";
}
