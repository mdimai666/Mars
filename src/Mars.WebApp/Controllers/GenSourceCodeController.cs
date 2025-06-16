using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

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
