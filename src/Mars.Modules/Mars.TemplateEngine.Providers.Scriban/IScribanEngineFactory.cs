using Scriban.Runtime;

namespace Mars.TemplateEngine.Providers.ScribanProvider;

/// <summary>
/// Создаёт глобальные <see cref="ScriptObject"/> с функциями — единая точка конфигурации
/// движка библиотеки для всех потребителей (нода Template, сайт-движки, плагины).
/// </summary>
public interface IScribanEngineFactory
{
    /// <param name="scope">Область применения из <see cref="ScribanScopes"/> — по ней отбираются контрибьюторы.</param>
    ScriptObject CreateGlobalObject(string scope);
}

public class ScribanEngineFactory(IEnumerable<IScribanObjectContributor> contributors) : IScribanEngineFactory
{
    public ScriptObject CreateGlobalObject(string scope)
    {
        var scriptObject = new ScriptObject();

        foreach (var contributor in contributors)
        {
            if (contributor.Scope is null
                || string.Equals(contributor.Scope, scope, StringComparison.OrdinalIgnoreCase))
            {
                contributor.Configure(scriptObject);
            }
        }

        return scriptObject;
    }
}
