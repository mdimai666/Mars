using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Integration.Tests.TestControllers;

[ApiController]
[Route("api-test/[controller]/[action]")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class TestApi1Controller : ControllerBase
{
    private readonly IRequestContext _requestContext;

    public TestApi1Controller(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    public object CheckRequestContext()
    {
        _ = _requestContext.User?.Id;

        return new
        {
            _requestContext.Jwt,
            Claims = _requestContext.Claims.Claims.Select(s => new KeyValuePair<string, string>(s.Type, s.Value)),
            _requestContext.Roles,
            _requestContext.User,
            _requestContext.IsAuthenticated,
            _requestContext.UserName,
        };
    }

    public TimeOnly TimeOnlyResponse()
    {
        return new TimeOnly(8, 12, 16);
    }

    public DateOnly DateOnlyResponse()
    {
        return new DateOnly(2022, 6, 22);
    }
}
