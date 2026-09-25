using HandlebarsDotNet;
using HandlebarsDotNet.Extension.Json;
using HandlebarsDotNet.Extension.NewtonsoftJson;

namespace Mars.TemplateEngine.Providers.HandlebarsProvider;

/// <summary>
/// Создаёт настроенные экземпляры <see cref="IHandlebars"/> — единая точка конфигурации
/// движка библиотеки для всех потребителей (нода Template, сайт-движки, плагины).
/// </summary>
public interface IHandlebarsEngineFactory
{
    /// <param name="scope">Область применения из <see cref="HandlebarsScopes"/> — по ней отбираются контрибьюторы.</param>
    /// <param name="configureConfiguration">Дополнительная настройка конфигурации до создания экземпляра.</param>
    IHandlebars Create(string scope, Action<HandlebarsConfiguration>? configureConfiguration = null);
}

public class HandlebarsEngineFactory(IEnumerable<IHandlebarsBuilderContributor> contributors) : IHandlebarsEngineFactory
{
    public IHandlebars Create(string scope, Action<HandlebarsConfiguration>? configureConfiguration = null)
    {
        var configuration = new HandlebarsConfiguration();
        configureConfiguration?.Invoke(configuration);

        var handlebars = Handlebars.Create(configuration);
        handlebars.Configuration.UseJson();
        handlebars.Configuration.UseNewtonsoftJson();

        foreach (var contributor in contributors)
        {
            if (contributor.Scope is null
                || string.Equals(contributor.Scope, scope, StringComparison.OrdinalIgnoreCase))
            {
                contributor.Configure(handlebars);
            }
        }

        return handlebars;
    }
}
