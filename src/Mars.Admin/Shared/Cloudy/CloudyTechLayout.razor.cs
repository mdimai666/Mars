using Mars.Admin.Shared.ActionCenter;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Admin.Shared.Cloudy;

public partial class CloudyTechLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] HotKeys HotKeys { get; set; } = default!;
    [Inject] ActionCenterService ActionCenterService { get; set; } = default!;

    HotKeysContext HotKeysContext = default!;

    protected override void OnInitialized()
    {
        HotKeysContext = HotKeys.CreateContext()
             .Add(ModCode.None, Code.F1, () => ActionCenterService.Toggle(), "Open Action center")
             .Add(ModCode.Ctrl, Code.K, () => ActionCenterService.Toggle(), "Open Action center");
    }

    public async ValueTask DisposeAsync()
    {
        if (HotKeysContext is not null)
            await HotKeysContext.DisposeAsync();
    }
}
