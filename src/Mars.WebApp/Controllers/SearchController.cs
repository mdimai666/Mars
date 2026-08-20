using System.Diagnostics;
using System.Net.Mime;
using Mars.Core.Exceptions;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.Search;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class SearchController : ControllerBase
{
    private readonly ICentralSearchService _centralSearchService;

    public SearchController(ICentralSearchService centralSearchService)
    {
        _centralSearchService = centralSearchService;
    }

    [HttpGet("Query")]
    [Authorize]
    public async Task<IReadOnlyCollection<SearchFoundElementResponse>> Query(string? text, int maxCount = 10, CancellationToken cancellationToken = default)
    {
        if (maxCount > 30) throw MarsValidationException.FromSingleError(nameof(maxCount), "maxCount maximum is 30");
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2) return [];
        var results = await _centralSearchService.ActionBarSearch(text, maxCount, cancellationToken);
        return results.ToResponse();
    }
}
