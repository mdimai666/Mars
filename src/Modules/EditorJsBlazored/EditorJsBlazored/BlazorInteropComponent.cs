using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EditorJsBlazored;

public class BlazorInteropComponent : ComponentBase
{
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

    protected ElementReference DomElement { get; set; }
    protected Guid ClientComponentID { get; set; } = Guid.NewGuid();


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await this.InitializeClientComponent();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected virtual Task InitializeClientComponent()
    {
        return Task.CompletedTask;
    }

    protected async Task CallClientMethod(string methodName, params object[] args)
    {
        var allParams = new object[] { ClientComponentID, methodName }.ToList();

        allParams.AddRange(args.ToList());

        await JSRuntime.InvokeAsync<string>($"editorJsHandler.callEditorMethod", allParams.ToArray());
    }

    /// <summary>
    /// Method invoked from JS that retrieves Blazor component property value.
    /// </summary>
    /// <param name="propertyName">Name of the property to retrieve</param>
    /// <returns>Vale of the property</returns>
    [JSInvokable]
    public Task<object> GetBlazorProperty(string propertyName)
    {
        var property = this.GetType().GetProperty(propertyName);

        if (property == null)
        {
            throw new ApplicationException($"Property {propertyName} not found on {this.GetType().Name}");
        }

        return Task.FromResult(property.GetValue(this))!;
    }
}