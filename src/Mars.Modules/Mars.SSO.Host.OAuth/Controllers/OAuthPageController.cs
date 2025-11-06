using Mars.Host.Shared.Features;
using Mars.SSO.Host.OAuth.interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FeatureManagement.Mvc;

namespace Mars.SSO.Host.OAuth.Controllers;

[Controller]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/openid-connect", Name = "oauth")]
[FeatureGate(FeatureFlags.SingleSignOn)]
public class OAuthPageController : Controller
{
    const string ViewPath = "~/Views/SSO/";
    const string LoginPageView = ViewPath + "LoginPage.cshtml";
    const string InvalidRequestPageView = ViewPath + "InvalidRequestPage.cshtml";
    public const string LoginPageUrl = "/api/openid-connect/auth";
    private readonly IOAuthClientStore _clients;
    private readonly IOAuthService _oAuthService;

    public OAuthPageController(IOAuthClientStore clients, IOAuthService oAuthService)
    {
        _clients = clients;
        _oAuthService = oAuthService;
    }

    [HttpGet("auth")]
    //public async Task<IActionResult> LoginPage(string client_id, string redirect_uri, string scope, string response_type, string? state, string? nonce)
    public async Task<IActionResult> LoginPage(string client_id, Guid credential_id, CancellationToken cancellationToken)
    {
        var auth = await _oAuthService.FindAuthByIdAsync(credential_id, cancellationToken);
        if (auth == null)
            return View(InvalidRequestPageView);
        //var parameters = new[] { client_id, redirect_uri, response_type };
        //if (parameters.Any(string.IsNullOrWhiteSpace))
        //    return View(InvalidRequestPageView);

        //var client = _clients.FindClientById(client_id);
        //if (client == null)
        //    return BadRequest("Unknown client");

        //var (code, cred) =  await _oAuthService.CreateAuthorizationCodeAsync(client_id, redirect_uri,)

        //ViewData["ClientId"] = client_id;
        //ViewData["RedirectUri"] = redirect_uri;
        //ViewData["Scope"] = scope;
        //ViewData["State"] = state;
        //ViewData["Nonce"] = nonce;
        //ViewData["Request.Query"] = Request.Query;
        ViewData["credential_id"] = credential_id;
        return View(LoginPageView);
    }

    [HttpPost("auth")]
    //public IActionResult Login(string username, string password, string redirect_uri, string? state)
    public async Task<IActionResult> Login(string username, string password, Guid credential_id, CancellationToken cancellationToken)
    {
        var auth = await _oAuthService.FindAuthByIdAsync(credential_id, cancellationToken);
        if (auth == null)
            return View(InvalidRequestPageView);

        //var parameters = new[] { username, password, redirect_uri };
        //if (parameters.Any(string.IsNullOrWhiteSpace))
        //    return View(InvalidRequestPageView);

        //if (username == "demo" && password == "demo")
        //{
        //    var code = "demo-code";
        //    return Redirect($"{redirect_uri}?code={code}&state={state}");
        //}
        //return View("Index");

        if (await _oAuthService.AuthorizeSubjectAndUpdateAuthCode(username, password, credential_id, cancellationToken))
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            string redirectUrl = QueryHelpers.AddQueryString(auth.RedirectUri!, new Dictionary<string, string?>
            {
                ["code"] = auth.Code,
                ["state"] = auth.State,
                ["client_id"] = auth.ClientId,
                ["redirect_uri"] = auth.RedirectUri,
            });
            //return Redirect(auth.RedirectUri + $"?code={auth.Code}&state={auth.State}");

            return Redirect(redirectUrl);
        }

        ViewData["ErrorMessage"] = "Неверное имя пользователя или пароль.";

        //ViewData["Request.Query"] = Request.Query;
        ViewData["credential_id"] = credential_id;
        ViewData["username"] = username;
        return View(LoginPageView);
    }

}
