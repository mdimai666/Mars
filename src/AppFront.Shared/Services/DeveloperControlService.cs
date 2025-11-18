using AppFront.Shared.Tools;
using Mars.Shared.Tools;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppFront.Shared.Services;

public class DeveloperControlService
{
    private readonly NavigationManager navigationManager;
    private readonly ModelInfoService modelService;

    static List<GPageInfo> pages = default!;

    MyJS js;

    public DeveloperControlService(NavigationManager navigationManager, ModelInfoService modelService, IJSRuntime JS)
    {
        this.navigationManager = navigationManager;
        this.modelService = modelService;

        if (pages == null)
        {
            if (Q.Program is null)
            {
                pages = new();
            }
            else
            {
                pages = modelService.GetPages(Q.Program.GetType().Assembly);
            }

        }
        js = new MyJS(JS);
    }

    public void OpenPageSource()
    {
        var filename = modelService.GetFileNameFromPageClass(navigationManager, pages);
        var path = Q.WorkDir;

        if (filename is null)
        {
            Console.Error.WriteLine("filename is null");
            return;
        }

        var x = Q.HostingInfo.NormalizedPathJoin(path, filename);
        _ = js.OpenNewTab($"vs2026://{x}");
    }

    public void OpenPageSource(Type pageType, string? prependPath = null)
    {
        string filename = modelService.GetFileNameFromPageClass(pageType);
        string path = Q.WorkDir;

        if (filename is null)
        {
            Console.Error.WriteLine("filename is null");
            return;
        };

        var x = Q.HostingInfo.NormalizedPathJoin(path, prependPath, filename);
        _ = js.OpenNewTab($"vs2026://{x}");
    }
}
