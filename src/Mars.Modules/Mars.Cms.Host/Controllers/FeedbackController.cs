using System.Net.Mime;
using Mars.Cms.Abstractions.Dto.Feedbacks;
using Mars.Cms.Abstractions.Mappings.Feedbacks;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.Feedbacks;
using Mars.Contracts.Common;
using Mars.Core.Constants;
using Mars.Core.Exceptions;
using Mars.Excel.Abstractions;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Cms.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly IRequestContext _requestContext;
    private readonly IExcelService _excelService;

    public FeedbackController(
        IFeedbackService feedbackService,
        IRequestContext requestContext,
        IExcelService excelService)
    {
        _feedbackService = feedbackService;
        _requestContext = requestContext;
        _excelService = excelService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<FeedbackDetailResponse> Get(Guid id, CancellationToken cancellationToken)
    {
        return (await _feedbackService.GetDetail(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();
    }

    [HttpPost]
    [AllowAnonymous]
    //[EnableRateLimiting()]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateFeedbackRequest request, CancellationToken cancellationToken)
    {
        var created = await _feedbackService.Create(request.ToQuery(_requestContext.User?.Id), cancellationToken);
        return Created("{id}", created.Id);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FeedbackDetailResponse))]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public async Task<FeedbackDetailResponse> Update([FromBody] UpdateFeedbackRequest request, CancellationToken cancellationToken)
    {
        return (await _feedbackService.Update(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("list/offset")]
    public async Task<ListDataResult<FeedbackSummaryResponse>> List([FromQuery] ListFeedbackQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _feedbackService.List(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpGet("list/page")]
    public async Task<PagingResult<FeedbackSummaryResponse>> ListTable([FromQuery] TableFeedbackQueryRequest request, CancellationToken cancellationToken)
    {
        return (await _feedbackService.ListTable(request.ToQuery(), cancellationToken)).ToResponse();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task Delete(Guid id, CancellationToken cancellationToken)
    {
        return _feedbackService.Delete(id, cancellationToken);
    }

    [HttpDelete("DeleteMany")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task DeleteMany([FromQuery] Guid[] ids, CancellationToken cancellationToken)
    {
        return _feedbackService.DeleteMany(new DeleteManyFeedbackQuery { Ids = ids }, cancellationToken);
    }

    [HttpGet("DownloadExcel")]
    public async Task<FileContentResult> DownloadExcel(CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        await _feedbackService.ExcelFeedbackList(stream, cancellationToken);

        return _excelService.ExcelResponse(this, stream, "Feedback.xlsx");
    }

}
