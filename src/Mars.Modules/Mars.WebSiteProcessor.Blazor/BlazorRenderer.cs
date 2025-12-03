using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;

namespace Mars.WebSiteProcessor.Blazor;

// В разработке.
// Не работает когда есть авторизация итд.
// Пока не используется.
internal class BlazorRenderer
{
    private readonly HtmlRenderer _htmlRenderer;
    public BlazorRenderer(HtmlRenderer htmlRenderer)
    {
        _htmlRenderer = htmlRenderer;
    }

    public Task<string> RenderComponent<T>() where T : IComponent
        => RenderComponent<T>(ParameterView.Empty);

    public Task<string> RenderComponent<T>(Dictionary<string, object?> dictionary) where T : IComponent
        => RenderComponent<T>(ParameterView.FromDictionary(dictionary));

    private Task<string> RenderComponent<T>(ParameterView parameters) where T : IComponent
    {
        return _htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await _htmlRenderer.RenderComponentAsync<T>(parameters);
            return output.ToHtmlString();
        });
    }

    public Task<string> RenderComponent(Type type)
        => RenderComponent(type, ParameterView.Empty);

    public Task<string> RenderComponent(Type type, Dictionary<string, object?> dictionary)
        => RenderComponent(type, ParameterView.FromDictionary(dictionary));

    private Task<string> RenderComponent(Type type, ParameterView parameters)
    {
        return _htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await _htmlRenderer.RenderComponentAsync(type, parameters);
            return output.ToHtmlString();
        });
    }
}
