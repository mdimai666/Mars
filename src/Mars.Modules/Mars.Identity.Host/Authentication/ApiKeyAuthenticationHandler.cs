using System.Security.Claims;
using System.Text.Encodings.Web;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mars.Identity.Host.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-API-Key";

    private readonly IUserApiKeyRepository _apiKeyRepository;
    private readonly UserManager<UserEntity> _userManager;
    private readonly IUserClaimsPrincipalFactory<UserEntity> _claimsPrincipalFactory;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserApiKeyRepository apiKeyRepository,
        UserManager<UserEntity> userManager,
        IUserClaimsPrincipalFactory<UserEntity> claimsPrincipalFactory)
        : base(options, logger, encoder)
    {
        _apiKeyRepository = apiKeyRepository;
        _userManager = userManager;
        _claimsPrincipalFactory = claimsPrincipalFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue))
            return AuthenticateResult.NoResult();

        if (!ApiKeyFormat.TryParse(headerValue.FirstOrDefault(), out var keyId, out var secret))
            return AuthenticateResult.Fail("Invalid API key format");

        var storedKey = await _apiKeyRepository.GetForValidation(keyId, Context.RequestAborted);
        if (storedKey is null)
            return AuthenticateResult.Fail("API key not found");

        if (storedKey.ExpiresAt.HasValue && storedKey.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            return AuthenticateResult.Fail("API key expired");

        if (!ApiKeyFormat.SecretMatchesHash(secret, storedKey.KeyHash))
            return AuthenticateResult.Fail("Invalid API key");

        var user = await _userManager.FindByIdAsync(storedKey.UserId.ToString());
        if (user is null)
            return AuthenticateResult.Fail("API key user not found");

        var userPrincipal = await _claimsPrincipalFactory.CreateAsync(user);
        var identity = new ClaimsIdentity(userPrincipal.Claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
