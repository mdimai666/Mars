namespace Mars.Host.Shared.WebSite;

/// <summary>
/// Маркер endpoint'а, который рендерит контент фронтов (публичное API рендера).
/// Такие endpoint'ы проходят через обработчики пайплайна фронтов
/// (<see cref="IFrontRequestHandler"/>) наравне со страницами фронтов —
/// например, закрываются в режиме обслуживания, если опция это включает.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class FrontRenderEndpointAttribute : Attribute
{
}
