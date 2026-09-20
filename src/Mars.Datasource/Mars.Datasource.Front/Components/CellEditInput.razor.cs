using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Datasource.Front.Components;

/// <summary>
/// Инпут правки ячейки: получает фокус сам на своём первом рендере. Ссылка на элемент принадлежит
/// этому экземпляру, поэтому чужой дифф (правка переехала на другую ячейку) не может оставить её
/// пустой — общий <c>@ref</c> в таблице на удалении старого инпута сбрасывался в default,
/// и <c>FocusAsync</c> падал с «ElementReference has not been configured correctly».
/// </summary>
public partial class CellEditInput : ComponentBase
{
    ElementReference _input;

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }
    [Parameter] public EventCallback OnBlur { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await _input.FocusAsync();
    }
}
