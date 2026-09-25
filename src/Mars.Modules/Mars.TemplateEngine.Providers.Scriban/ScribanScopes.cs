namespace Mars.TemplateEngine.Providers.ScribanProvider;

/// <summary>
/// Области применения <see cref="IScribanObjectContributor"/>:
/// Core — движок шаблонов нод (Core.Scriban), Site — движок рендера сайта.
/// </summary>
public static class ScribanScopes
{
    public const string Core = "core";
    public const string Site = "site";
}
