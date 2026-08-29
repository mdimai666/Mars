using System.Net.Mime;
using Mars.Cms.Abstractions.Services;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Cms.Host.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class GenSourceCodeController : ControllerBase
{
    readonly IMetaModelTypesLocator _mlocator;

    public GenSourceCodeController(IMetaModelTypesLocator mlocator)
    {
        _mlocator = mlocator;
    }

    [HttpGet]
    [Produces(MediaTypeNames.Text.Plain)]
    [ProducesErrorResponseType(typeof(void))]
    public Task<string> MetaTypesSourceCode(string lang = "csharp")
        => _mlocator.MetaTypesSourceCode(lang);
}
