using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.WebApp.Nodes.Front.Models.AppEntityForms;
using Mars.WebApp.Nodes.Host.Builders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.WebApp.Nodes.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class WebAppNodesAppEntityController : ControllerBase
{
    private readonly IAppEntityFormBuilderFactory _formBuilderFactory;

    public WebAppNodesAppEntityController(IAppEntityFormBuilderFactory formBuilderFactory)
    {
        _formBuilderFactory = formBuilderFactory;
    }

    [HttpGet("FormBuilder/CreateFormsBuilderDictionary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public AppEntityCreateFormsBuilderDictionary GetFormBuilder(CancellationToken cancellationToken)
    {
        return _formBuilderFactory.FormsBuilderDictionary();
    }
}
