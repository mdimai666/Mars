using System.Net.Mime;
using Mars.Identity.Contracts.ViewModels;
using Mars.Server.Abstractions.Authorization;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.WebApp.Nodes.Host.Builders;
using Mars.WebApp.Nodes.Models.AppEntityForms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.WebApp.Nodes.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Cap(RoleCaps.FlowCap.Manage)]
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
