using System.Text;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.SSO.Dto;
using Mars.SSO.Host.OAuth.interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Mars.SSO.Host.OAuth.Controllers;

[ApiController]
[Route("api/oauth", Name = "oauth")]
public class OAuthHostController : ControllerBase
{
    private readonly IOAuthService _oauth;
    private readonly IRequestContext _requestContext;

    public OAuthHostController(IOAuthService oauth, IRequestContext requestContext)
    {
        _oauth = oauth;
        _requestContext = requestContext;
    }

    // Authorization endpoint (GET) - Authorization Code flow
    // Example request:
    // GET /oauth/authorize?response_type=code&client_id=xxx&redirect_uri=https://...&scope=openid%20email&state=abc&code_challenge=...&code_challenge_method=S256
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string response_type,
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string scope,
        [FromQuery] string state,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        CancellationToken cancellationToken)
    {
        // validate response_type
        if (response_type != "code") return BadRequest("unsupported_response_type");
        var scopes = scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        if (_requestContext.IsAuthenticated)
        {
            var (code, auth) = await _oauth.CreateAuthorizationCodeAsync(client_id, redirect_uri, state, _requestContext.User.Id, code_challenge ?? "", code_challenge_method ?? "S256", scopes, cancellationToken);

            string redirectUrl = QueryHelpers.AddQueryString(auth.RedirectUri!, new Dictionary<string, string?>
            {
                ["code"] = auth.Code,
                ["state"] = auth.State,
                ["client_id"] = auth.ClientId,
                ["redirect_uri"] = auth.RedirectUri,
            });
            return Redirect(redirectUrl);
        }
        else
        {
            var (code, auth) = await _oauth.CreateAuthorizationCodeAsync(client_id, redirect_uri, state, Guid.Empty, code_challenge ?? "", code_challenge_method ?? "S256", scopes, cancellationToken);

            string redirectUrl = QueryHelpers.AddQueryString(baseUrl + OAuthPageController.LoginPageUrl, new Dictionary<string, string?>
            {
                ["code"] = code,
                ["state"] = state,
                ["client_id"] = client_id,
                ["credential_id"] = auth.Id.ToString(),
            });
            return Redirect(redirectUrl);
        }
    }

    // Token endpoint (POST)
    // for Authorization Code: grant_type=authorization_code&code=...&redirect_uri=...&client_id=...&client_secret=...&code_verifier=...
    // for Refresh token: grant_type=refresh_token&refresh_token=...&client_id=...&client_secret=...
    // for client_credentials: grant_type=client_credentials&client_id=...&client_secret=...&scope=...
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] IFormCollection form, CancellationToken cancellationToken)
    {
        var grantType = form["grant_type"].FirstOrDefault();
        if (string.IsNullOrEmpty(grantType)) return BadRequest("invalid_request");

        // Basic auth client credentials are allowed; also allow client_id/client_secret in body
        string? clientId = null;
        string? clientSecret = null;
        // Try HTTP Basic
        var enableBasicAuth = false;
        if (enableBasicAuth)
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var cred = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader["Basic ".Length..].Trim()));
                var idx = cred.IndexOf(':');
                if (idx > -1)
                {
                    clientId = cred.Substring(0, idx);
                    clientSecret = cred.Substring(idx + 1);
                }
            }
        }
        // fallback to body
        clientId ??= form["client_id"].FirstOrDefault();
        clientSecret ??= form["client_secret"].FirstOrDefault();
        var state = form["state"].FirstOrDefault();

        try
        {
            if (grantType == "authorization_code")
            {
                var code = form["code"].FirstOrDefault() ?? throw new InvalidOperationException("code required");
                var redirectUri = form["redirect_uri"].FirstOrDefault() ?? throw new InvalidOperationException("redirect_uri required");
                var codeVerifier = form["code_verifier"].FirstOrDefault();

                var (access, refresh, expiresIn) = await _oauth.ExchangeCodeForTokenAsync(code, clientId!, clientSecret!, redirectUri, state, codeVerifier, cancellationToken);
                return Ok(new OpenIdTokenResponse
                {
                    AccessToken = access,
                    TokenType = "Bearer",
                    ExpiresIn = expiresIn,
                    RefreshToken = refresh
                });
            }
            else if (grantType == "refresh_token")
            {
                var refresh = form["refresh_token"].FirstOrDefault() ?? throw new InvalidOperationException("refresh_token required");
                var (accessToken, newRefresh, expiresIn) = await _oauth.ExchangeRefreshTokenAsync(refresh, clientId!, clientSecret!, cancellationToken);
                return Ok(new
                {
                    access_token = accessToken,
                    token_type = "Bearer",
                    expires_in = expiresIn,
                    refresh_token = newRefresh
                });
            }
            //else if (grantType == "client_credentials")
            //{
            //    var scope = form["scope"].FirstOrDefault();
            //    var scopes = (scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            //    var (access, expiresIn) = await _oauth.ClientCredentialsAsync(clientId!, clientSecret!, scopes, cancellationToken);
            //    return Ok(new { access_token = access, token_type = "Bearer", expires_in = expiresIn });
            //}
            else if (grantType == "password")
            {
                var username = form["username"].FirstOrDefault() ?? throw new InvalidOperationException("username required");
                var password = form["password"].FirstOrDefault() ?? throw new InvalidOperationException("password required");
                var scope = form["scope"].FirstOrDefault();
                var scopes = (scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var (access, refresh, expiresIn) = await _oauth.PasswordGrantAsync(username, password, clientId!, clientSecret!, scopes, cancellationToken);
                return Ok(new OpenIdTokenResponse
                {
                    AccessToken = access,
                    TokenType = "Bearer",
                    ExpiresIn = expiresIn,
                    RefreshToken = refresh
                });
            }
            else
            {
                return BadRequest("unsupported_grant_type");
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "invalid_grant", error_description = ex.Message });
        }
    }

    // optional revoke endpoint (RFC7009)
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromForm] string token, [FromForm] string token_type_hint)
    {
        if (string.IsNullOrEmpty(token)) return BadRequest();
        await _oauth.RevokeRefreshTokenAsync(token);
        return Ok();
    }

    // userinfo endpoint (for OIDC)
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        if (!_requestContext.IsAuthenticated) return Unauthorized();

        var user = _requestContext.User;

        return Ok(new
        {
            sub = user.Id,
            email = user.Email,
            name = $"{user.FirstName} {user.LastName}".Trim(),
            given_name = user.FirstName,
            family_name = user.LastName
        });
    }
}
