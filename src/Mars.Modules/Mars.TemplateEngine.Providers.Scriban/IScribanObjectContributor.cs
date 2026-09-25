using Scriban.Runtime;

namespace Mars.TemplateEngine.Providers.ScribanProvider;

/// <summary>
/// Точка расширения: регистрирует функции/значения в глобальном <see cref="ScriptObject"/>,
/// созданном <see cref="IScribanEngineFactory"/>. Регистрируется в DI (IEnumerable) —
/// встроенные модули и плагины добавляют своих контрибьюторов.
/// </summary>
public interface IScribanObjectContributor
{
    /// <summary>
    /// Область применения (<see cref="ScribanScopes"/>). Null — применять ко всем областям.
    /// </summary>
    string? Scope { get; }

    void Configure(ScriptObject scriptObject);
}
