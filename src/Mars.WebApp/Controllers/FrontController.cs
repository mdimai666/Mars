using System.Net.Mime;
using Mars.Core.Exceptions;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.NavMenus;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.WebSite.Dto;
using Mars.Shared.Options;
using Mars.WebSiteProcessor.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class FrontController : ControllerBase
{
    private readonly IMarsAppProvider _marsAppProvider;
    private readonly IFrontManager _frontManager;
    private readonly FrontTemplateService _frontTemplateService;
    private readonly FrontFilesService _frontFilesService;
    private readonly IWebRenderEngineLocator _renderEngineLocator;
    private readonly IOptionService _optionService;

    public FrontController(
        IMarsAppProvider MarsAppProvider,
        IFrontManager frontManager,
        FrontTemplateService frontTemplateService,
        FrontFilesService frontFilesService,
        IWebRenderEngineLocator renderEngineLocator,
        IOptionService optionService)
    {
        _marsAppProvider = MarsAppProvider;
        _frontManager = frontManager;
        _frontTemplateService = frontTemplateService;
        _frontFilesService = frontFilesService;
        _renderEngineLocator = renderEngineLocator;
        _optionService = optionService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FMarsAppFrontTemplateMinimumResponse FrontMinimal()
    {
        var app = _marsAppProvider.FirstApp;
        var ts = app.Features.Get<IWebTemplateService>();

        return ts.Template.ToMinimumResponse();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FMarsAppFrontTemplateSummaryResponse FrontFiles()
    {
        var app = _marsAppProvider.FirstApp;
        var ts = app.Features.Get<IWebTemplateService>();

        return ts.Template.ToSummaryResponse();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FrontSummaryInfoResponse FrontSummaryInfo()
    {
        var app = _marsAppProvider.FirstApp;
        var ts = app.Features.Get<IWebTemplateService>();

        return new FrontSummaryInfoResponse
        {
            Mode = app.Configuration.Mode,
            PagesCount = ts.Template.Pages.Count,
            PartsCount = ts.Template.Parts.Count,
        };
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public FWebPartResponse? GetPart(string fileRelPath)
    {
        var app = _marsAppProvider.FirstApp;
        var ts = app.Features.Get<IWebTemplateService>();

        var page = ts.Template.Pages.FirstOrDefault(x => x.FileRelPath == fileRelPath);

        if (page != null)
        {
            return page.ToPartResponse();
        }

        var part = ts.Template.Parts.FirstOrDefault(x => x.FileRelPath == fileRelPath);

        if (part == null) throw new NotFoundException();

        return part.ToResponse();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IReadOnlyCollection<FFrontEngineResponse> Engines()
    {
        return _renderEngineLocator.GetAvailableEngines()
            .Select(s => new FFrontEngineResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
            })
            .ToList();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public UserActionResult CreateFront([FromBody] FCreateFrontRequest request)
    {
        if (!FrontManager.IsValidSlug(request.Slug))
            return UserActionResult.Exception($"Некорректный slug '{request.Slug}'", null);

        var option = _optionService.GetOption<FrontsOption>();
        if (option.Fronts.Any(s => string.Equals(s.Slug, request.Slug, StringComparison.OrdinalIgnoreCase)))
            return UserActionResult.Exception($"Фронт '{request.Slug}' уже существует", null);

        if (request.UseTemplate)
        {
            _frontTemplateService.CreateFrontFromTemplate(request.Slug);
        }
        else
        {
            Directory.CreateDirectory(Path.Combine(_frontTemplateService.FrontsBasePath, request.Slug));
        }

        option.Fronts.Add(new FrontItem
        {
            Slug = request.Slug,
            Title = string.IsNullOrWhiteSpace(request.Title) ? request.Slug : request.Title,
            Url = request.Url,
            Path = "",
            EngineId = FrontItem.HandlebarsEngine,
            Enabled = true,
        });
        _optionService.SaveOption(option);

        return UserActionResult.Success($"Фронт '{request.Slug}' создан");
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public UserActionResult DeleteFront(string slug, bool deleteFolder = false)
    {
        var option = _optionService.GetOption<FrontsOption>();
        var front = option.Fronts.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException($"Front '{slug}' not found");

        option.Fronts.Remove(front);
        _optionService.SaveOption(option);

        // внешние папки (Path задан) через API не удаляются
        if (deleteFolder && string.IsNullOrWhiteSpace(front.Path))
        {
            var dir = Path.Combine(_frontTemplateService.FrontsBasePath, slug);
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }

        return UserActionResult.SuccessDeleted();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FFrontTreeNodeResponse FrontTree(string slug)
    {
        return _frontFilesService.GetTree(slug);
    }

    /// <summary>
    /// Страницы фронта: файл → URL из атрибута @page (для «открыть в предпросмотре» в редакторе).
    /// Root-файлы (_root.hbs) тоже попадают сюда — их URL это StartPath.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IReadOnlyCollection<FFrontPageResponse> FrontPages(string slug)
    {
        var app = _renderEngineLocator.GetAppFrontBySlug(slug);
        if (app is null) return [];

        var ts = app.Features.Get<IWebTemplateService>();
        var pages = ts.Template.Pages
            .Select(s => new FFrontPageResponse
            {
                FileRelPath = s.FileRelPath,
                Url = s.Url,
            });
        var roots = ts.Template.Roots.Values
            .Select(s => new FFrontPageResponse
            {
                FileRelPath = s.FileRelPath,
                Url = s.StartPath,
            });

        return [.. pages, .. roots];
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FFrontFileContentResponse ReadFrontFile(string slug, string relPath)
    {
        return _frontFilesService.ReadFile(slug, relPath);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public UserActionResult SaveFrontFile(string slug, string relPath, [FromBody] string content)
    {
        _frontFilesService.SaveFile(slug, relPath, content);
        return UserActionResult.Success();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public UserActionResult CreateFrontFile(string slug, string relPath, bool isFolder = false)
    {
        if (isFolder) _frontFilesService.CreateFolder(slug, relPath);
        else _frontFilesService.CreateFile(slug, relPath);
        return UserActionResult.Success();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public UserActionResult RenameFrontFile(string slug, string relPath, string newRelPath)
    {
        _frontFilesService.Rename(slug, relPath, newRelPath);
        return UserActionResult.Success();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public UserActionResult DeleteFrontFile(string slug, string relPath)
    {
        _frontFilesService.Delete(slug, relPath);
        return UserActionResult.SuccessDeleted();
    }
}
