using HandlebarsDotNet;

namespace Mars.TemplateEngine.Providers.HandlebarsProvider;

/// <summary>
/// Точка расширения: регистрирует хелперы/форматтеры на экземпляре Handlebars,
/// созданном <see cref="IHandlebarsEngineFactory"/>. Регистрируется в DI
/// (IEnumerable) — встроенные модули и плагины добавляют своих контрибьюторов.
/// </summary>
public interface IHandlebarsBuilderContributor
{
    /// <summary>
    /// Область применения (<see cref="HandlebarsScopes"/>). Null — применять ко всем областям.
    /// </summary>
    string? Scope { get; }

    void Configure(IHandlebars handlebars);
}
