using AppFront.Shared.Interfaces;
using AppFront.Shared.Tools;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppFront.Shared.Services;

public class DeveloperControlService
{
    private readonly NavigationManager navigationManager;
    private readonly IBlazorPagesService pagesService;

    MyJS js;

    public DeveloperControlService(NavigationManager navigationManager, IBlazorPagesService pagesService, IJSRuntime JS)
    {
        this.navigationManager = navigationManager;
        this.pagesService = pagesService;

        js = new MyJS(JS);
    }

    public void OpenPageSource()
    {
        if (Q.Program is null)
        {
            Console.Error.WriteLine("Q.Program is null");
            return;
        }

        // Путь относительно базы приложения (без префикса маунта)
        var relativeUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        var page = pagesService.FindPageByUrl([Q.Program.Assembly], relativeUrl);

        if (page is null)
        {
            Console.Error.WriteLine("page not found for url: " + navigationManager.Uri);
            return;
        }

        OpenInEditor(page.PageType);
    }

    public void OpenPageSource(Type pageType, string? prependPath = null)
    {
        OpenInEditor(pageType, prependPath);
    }

    private void OpenInEditor(Type pageType, string? prependPath = null)
    {
        var filename = pagesService.ResolveSourceFilePath(pageType);

        if (filename is null)
        {
            Console.Error.WriteLine("filename is null for type: " + pageType.FullName);
            return;
        }

        // Абсолютный путь (Debug на сервере) открываем как есть,
        // относительный склеиваем с рабочей директорией
        string target;
        if (Path.IsPathRooted(filename))
        {
            target = Q.HostingInfo.NormalizedPathJoin(filename);
        }
        else
        {
            target = Q.HostingInfo.NormalizedPathJoin(Q.WorkDir, prependPath, filename);
        }

        _ = js.OpenNewTab($"vs2026://{target}");
    }
}
