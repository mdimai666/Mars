using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.Contracts.XActions;

/// <summary>
/// Рекомендованный следующий шаг после выполнения действия. Эффекты — рекомендации,
/// а не команды: вызывающий сам решает, что с ними делать (без автоцепочек).
/// Неизвестные kind игнорируются (форвард-совместимость).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NavigateEffect), "navigate")]
[JsonDerivedType(typeof(NextActionEffect), "nextAction")]
[JsonDerivedType(typeof(TriggerEventEffect), "event")]
[JsonDerivedType(typeof(CustomEffect), "custom")]
public abstract record XActionEffect;

/// <summary>
/// Перейти по URL.
/// </summary>
public sealed record NavigateEffect(string Url) : XActionEffect;

/// <summary>
/// Рекомендованное следующее действие. Автоматически не выполняется —
/// решение принимает вызывающий (компонент UI, узел потока).
/// </summary>
public sealed record NextActionEffect(string ActionId, IReadOnlyDictionary<string, string>? Args = null) : XActionEffect;

/// <summary>
/// Поднять событие на клиентской шине.
/// </summary>
public sealed record TriggerEventEffect(string Name, JsonElement? Payload = null) : XActionEffect;

/// <summary>
/// Люк расширяемости для плагинов: плагин сам договаривается со своим фронтом
/// о значении CustomKind (ядро пользуется типизированными эффектами).
/// </summary>
public sealed record CustomEffect(string CustomKind, JsonElement Data) : XActionEffect;
