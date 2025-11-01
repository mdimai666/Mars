using Mars.SSO.Host.OAuth.Models;

namespace Mars.SSO.Host.OAuth.interfaces;

public interface IOAuthService
{
    // authorize flow: create auth code and return redirect URL or code (server does redirect)
    Task<(string code, AuthCode authCode)> CreateAuthorizationCodeAsync(string clientId,
                        string redirectUri, string? state, Guid subjectId, string codeChallenge,
                        string codeChallengeMethod, IEnumerable<string> scopes,
                        CancellationToken cancellationToken);

    Task<AuthCode?> FindAuthCodeAsync(string code, CancellationToken cancellationToken);
    Task<AuthCode?> FindAuthByIdAsync(Guid id, CancellationToken cancellationToken);
    Task RemoveAuthCodeAsync(AuthCode authCode);

    // token flows:
    Task<(string accessToken, string? refreshToken, int expiresIn)> ExchangeCodeForTokenAsync(string code,
                        string clientId, string clientSecret, string redirectUri, string? state,
                        string codeVerifier, CancellationToken cancellationToken);
    Task<(string accessToken, string? refreshToken, int expiresIn)> ExchangeRefreshTokenAsync(string refreshToken,
                        string clientId, string clientSecret,
                        CancellationToken cancellationToken);
    Task<(string accessToken, int expiresIn)> ClientCredentialsAsync(string clientId, string clientSecret,
                        IEnumerable<string> scopes,
                        CancellationToken cancellationToken);
    Task<(string accessToken, string? refreshToken, int expiresIn)> PasswordGrantAsync(string username, string password, string clientId, string clientSecret, IEnumerable<string> scopes, CancellationToken cancellationToken);

    // revoke
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task<bool> AuthorizeSubjectAndUpdateAuthCode(string username, string password, Guid credentialId, CancellationToken cancellationToken);
}
