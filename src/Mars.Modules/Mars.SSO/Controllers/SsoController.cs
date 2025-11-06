using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Features;
using Mars.Host.Shared.SSO.Services;
using Mars.Shared.Contracts.SSO;
using Mars.SSO.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Mars.SSO.Controllers;

[ApiController]
[Route("api/sso")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
[FeatureGate(FeatureFlags.SingleSignOn)]
public class SsoController : ControllerBase
{
    private readonly ISsoService _sso;

    public SsoController(ISsoService sso)
    {
        _sso = sso;
    }

    [HttpGet("providers")]
    public IEnumerable<SsoProviderItemResponse> ListProviders()
    {
        return _sso.Providers.Select(s => new SsoProviderItemResponse() { Name = s.Name, DisplayName = s.DisplayName, IconUrl = s.IconUrl });
    }

    [HttpGet("login/{provider}")]
    public IActionResult Login(string provider, [FromQuery] string redirectUri)
    {
        var p = _sso.GetProvider(provider);
        if (p == null) return NotFound();
        var state = Guid.NewGuid().ToString("N");
        var url = p.GetAuthorizationUrl(state, redirectUri);
        return Redirect(url);
    }

    [HttpGet("callback/{provider}")]
    public async Task<ActionResult<SsoUserInfoResponse>> Callback(string provider, [FromQuery] string code, [FromQuery] string redirectUri)
    {
        var info = await _sso.AuthenticateAsync(provider, code, redirectUri);
        if (info == null) return Unauthorized();

        return Ok(info.ToResponse());
    }
}
