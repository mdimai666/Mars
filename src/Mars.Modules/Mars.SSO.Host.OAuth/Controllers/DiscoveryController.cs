using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Mars.SSO.Host.OAuth.Controllers;

[Route(".well-known", Name = "oauth")]
public class DiscoveryController : Controller
{
    private readonly IConfiguration _config;
    private readonly IOptionService _optionService;

    public DiscoveryController(IConfiguration config, IOptionService optionService)
    {
        _config = config;
        _optionService = optionService;
    }

    [HttpGet("openid-configuration")]
    public IActionResult GetConfig()
    {
        //var issuer = _config["Jwt:Issuer"] ?? $"{Request.Scheme}://{Request.Host}";
        var issuer = _optionService.SysOption.SiteUrl;
        var baseUrl = issuer.TrimEnd('/');

        return Ok(new
        {
            issuer,
            authorization_endpoint = $"{baseUrl}/api/oauth/authorize",
            token_endpoint = $"{baseUrl}/api/oauth/token",
            userinfo_endpoint = $"{baseUrl}/api/oauth/userinfo",
            jwks_uri = $"{baseUrl}/.well-known/jwks.json",

            response_types_supported = new[] { "code", "id_token", "token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            grant_types_supported = new[] { "authorization_code", "refresh_token", "client_credentials", "password" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
            scopes_supported = new[] { "openid", "profile", "email", "offline_access" }
        });
    }
}
