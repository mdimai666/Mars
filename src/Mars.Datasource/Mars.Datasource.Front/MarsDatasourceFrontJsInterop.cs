using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Datasource.Front;

/// <summary>
/// JS-модуль рабочей области: копирование полных значений ячеек (см. <c>wwwroot/MarsDatasourceFrontJsInterop.js</c>).
/// Версия в урле — cache-busting по конвенции <c>ScriptFileInfo</c>: статика отдаётся без Cache-Control.
/// </summary>
public class MarsDatasourceFrontJsInterop : IAsyncDisposable
{
    private static string Version { get; } = Uri.EscapeDataString(
        typeof(MarsDatasourceFrontJsInterop).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0");

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public MarsDatasourceFrontJsInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", $"./_content/Mars.Datasource.Front/MarsDatasourceFrontJsInterop.js?v={Version}").AsTask());
    }

    /// <summary>Выделение внутри контейнера отдаёт полные значения ячеек, а не усечённые.</summary>
    public async ValueTask RegisterFullValueCopy(ElementReference container)
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("registerFullValueCopy", container);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Без модуля выделение копируется как обычно: полное значение всё равно доступно
            // в подсказке и в редакторе по двойному клику.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!moduleTask.IsValueCreated) return;

        try
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // страница выгружается, соединение с JS уже закрыто
        }
    }
}
