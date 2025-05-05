using System.Net.Mime;
using Mars.Core.Exceptions;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.NavMenus;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Interfaces;
using Mars.Shared.Contracts.WebSite.Dto;
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

    public FrontController(IMarsAppProvider MarsAppProvider)
    {
        _marsAppProvider = MarsAppProvider;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FMarsAppFrontTemplateMinimumResponse FrontMinimal()
    {
        var app = _marsAppProvider.FirstApp;
        var ts = app.Features.Get<IWebTemplateService>();
        //var renderEngine = appFront.Features.Get<IWebRenderEngine>();

        return ts.Template.ToMinimumResponse();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public FMarsAppFrontTemplateSummaryResponse FrontFiles()
    {
        var app = _marsAppProvider.FirstApp;
        var ts = app.Features.Get<IWebTemplateService>();
        //var renderEngine = appFront.Features.Get<IWebRenderEngine>();

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
}
