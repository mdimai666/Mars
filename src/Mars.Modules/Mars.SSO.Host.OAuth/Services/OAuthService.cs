using System.Security.Cryptography;
using System.Text;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.SSO.Host.OAuth.Data;
using Mars.SSO.Host.OAuth.interfaces;
using Mars.SSO.Host.OAuth.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars.SSO.Host.OAuth.Services;

public class OAuthService : IOAuthService
{
    private readonly SsoAuthDbContext _db;
    private readonly IOAuthClientStore _clientStore;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IExperimentalSignInService _experimentalSignInService;
    private readonly IAccountsService _accountsService;

    //private readonly LocalJwtService _jwtService;
    private readonly TimeSpan _defaultCodeLifetime = TimeSpan.FromMinutes(5);

    //public OAuthService(AuthDbContext db, LocalJwtService jwtService IOptionService optionService)
    public OAuthService(SsoAuthDbContext db, IOAuthClientStore clientStore,
                        ITokenService tokenService, IUserRepository userRepository,
                        IExperimentalSignInService experimentalSignInService,
                        IAccountsService accountsService)
    {
        _db = db;
        _clientStore = clientStore;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _experimentalSignInService = experimentalSignInService;
        _accountsService = accountsService;
        //_jwtService = jwtService;
    }

    public async Task<(string code, AuthCode authCode)> CreateAuthorizationCodeAsync(
                    string clientId, string redirectUri, string? state, Guid subjectId,
                    string codeChallenge, string codeChallengeMethod, IEnumerable<string> scopes,
                    CancellationToken cancellationToken)
    {
        //var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
        var client = _clientStore.FindClientById(clientId) ?? throw new InvalidOperationException("Unknown client");

        // validate redirectUri against registered
        var allowed = client.RedirectUris.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (client.RedirectUris != "*" && !allowed.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid redirect_uri");

        var code = CryptoRandomString(32); // helper to generate secure random
        var auth = new AuthCode
        {
            Code = code,
            ClientId = clientId,
            RedirectUri = redirectUri,
            State = state,
            SubjectId = subjectId,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            Scopes = string.Join(' ', scopes),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_defaultCodeLifetime)
        };

        _db.AuthCodes.Add(auth);
        await _db.SaveChangesAsync(cancellationToken);
        return (code, auth);
    }

    public Task<AuthCode?> FindAuthCodeAsync(string code, CancellationToken cancellationToken)
        => _db.AuthCodes.FirstOrDefaultAsync(c => c.Code == code && c.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public Task<AuthCode?> FindAuthByIdAsync(Guid id, CancellationToken cancellationToken)
        => _db.AuthCodes.FirstOrDefaultAsync(c => c.Id == id && c.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public async Task RemoveAuthCodeAsync(AuthCode authCode)
    {
        _db.AuthCodes.Remove(authCode);
        await _db.SaveChangesAsync();
    }

    public async Task<(string accessToken, string? refreshToken, int expiresIn)> ExchangeCodeForTokenAsync(
                        string code, string clientId, string clientSecret,
                        string redirectUri, string? state, string? codeVerifier,
                        CancellationToken cancellationToken)
    {
        var auth = await FindAuthCodeAsync(code, cancellationToken) ?? throw new InvalidOperationException("Invalid code");
        if (!string.Equals(auth.ClientId, clientId, StringComparison.Ordinal)) throw new InvalidOperationException("Client mismatch");
        if (!string.Equals(auth.RedirectUri, redirectUri, StringComparison.Ordinal)) throw new InvalidOperationException("redirect_uri mismatch");
        if (auth.ExpiresAt <= DateTime.UtcNow) throw new InvalidOperationException("Code expired");

        var client = _clientStore.FindClientById(clientId)
            ?? throw new InvalidOperationException("Unknown client");
        if (!client.AllowedGrantTypes.Contains("authorization_code"))
            throw new InvalidOperationException($"grant_type 'authorization_code' not allowed");

        // if confidential client => verify secret (omitted hashing example)
        //if (!string.IsNullOrEmpty(client.ClientSecretHash))
        if (!string.IsNullOrEmpty(client.ClientSecret))
        {
            if (clientSecret == null) throw new InvalidOperationException("Client secret required");
            // verify secret — here plain equality or hashed verify
            //if (!BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash))
            if (!client.VerifySecret(clientSecret))
                throw new InvalidOperationException("Invalid client secret");
        }

        // verify PKCE if required
        if (client.RequirePkce)
        {
            if (string.IsNullOrEmpty(auth.CodeChallenge)) throw new InvalidOperationException("No code challenge");
            ArgumentNullException.ThrowIfNull(codeVerifier, "Code verifier required");
            if (!VerifyPkce(auth.CodeChallengeMethod, auth.CodeChallenge!, codeVerifier!)) throw new InvalidOperationException("Invalid PKCE verifier");
        }

        // ok — create tokens
        var userId = auth.SubjectId; //??????????????????????????
        var scopes = auth.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //var expiresIn = client.AccessTokenLifetimeSeconds;
        var expiresIn = _tokenService.ExpiryInSeconds;
        //var accessToken = _jwtService.CreateToken(Guid.Parse(userId), "user@example.com", scopes); // adapt claims creation
        var accessToken = await _tokenService.CreateAccessToken(userId, _userRepository, cancellationToken);

        var all = _db.AuthCodes.ToList();

        string? refreshToken = null;
        //if (client.AllowOfflineAccess)
        if (true)
        {
            refreshToken = CryptoRandomString(64);
            var rt = new RefreshToken
            {
                Token = refreshToken,
                ClientId = client.ClientId,
                SubjectId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
                Revoked = false
            };
            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();
        }

        // remove used auth code
        await RemoveAuthCodeAsync(auth);

        return (accessToken, refreshToken, expiresIn);
    }

    public async Task<(string accessToken, string? refreshToken, int expiresIn)> ExchangeRefreshTokenAsync(
                            string refreshToken, string clientId,
                            string clientSecret, CancellationToken cancellationToken)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken && !x.Revoked && x.ExpiresAt > DateTime.UtcNow);
        if (rt == null) throw new InvalidOperationException("Invalid refresh token");
        if (!string.Equals(rt.ClientId, clientId, StringComparison.Ordinal)) throw new InvalidOperationException("Client mismatch");

        var client = _clientStore.FindClientById(clientId)
            ?? throw new InvalidOperationException("Unknown client");
        if (!client.AllowedGrantTypes.Contains("refresh_token"))
            throw new InvalidOperationException($"grant_type 'refresh_token' not allowed");
        //if (!string.IsNullOrEmpty(client.ClientSecretHash))
        if (!string.IsNullOrEmpty(client.ClientSecret))
        {
            if (clientSecret == null) throw new InvalidOperationException("Client secret required");
            //if (!BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash)) throw new InvalidOperationException("Invalid client secret");
            if (!client.VerifySecret(clientSecret))
                throw new InvalidOperationException("Invalid client secret");
        }

        // issue new access token (and optional new refresh token if rotate)
        //var accessToken = _jwtService.CreateToken(Guid.Parse(rt.SubjectId), "user@example.com", rt.ClientId.Split(' '));
        var accessToken = await _tokenService.CreateAccessToken(rt.SubjectId, _userRepository, cancellationToken);
        //var expiresIn = client.AccessTokenLifetimeSeconds;
        var expiresIn = _tokenService.ExpiryInSeconds;

        // optionally rotate refresh token: revoke old, create new
        rt.Revoked = true;
        var newRefresh = CryptoRandomString(64);
        var newRt = new RefreshToken
        {
            Token = newRefresh,
            ClientId = clientId,
            SubjectId = rt.SubjectId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn)
        };
        _db.RefreshTokens.Add(newRt);
        await _db.SaveChangesAsync();

        return (accessToken, newRefresh, expiresIn);
    }

    // Какая то мутная муть, кажется лишнее
    public async Task<(string accessToken, int expiresIn)> ClientCredentialsAsync(string clientId, string clientSecret, IEnumerable<string> scopes, CancellationToken cancellationToken)
    {
        var client = _clientStore.FindClientById(clientId)
            ?? throw new InvalidOperationException("Unknown client");
        //if (string.IsNullOrEmpty(client.ClientSecretHash)) throw new InvalidOperationException("Client is not confidential");
        if (string.IsNullOrEmpty(client.ClientSecret)) throw new InvalidOperationException("Client is not confidential");
        //if (!BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecretHash)) throw new InvalidOperationException("Invalid secret");
        if (!client.VerifySecret(clientSecret))
            throw new InvalidOperationException("Invalid secret");

        //var expiresIn = client.AccessTokenLifetimeSeconds;
        var expiresIn = _tokenService.ExpiryInSeconds;

        // For client credentials subject can be client id
        //var accessToken = _jwtService.CreateToken(Guid.NewGuid(), clientId, scopes); // subject id generation decision
        var accessToken = await _tokenService.CreateAccessToken(Guid.NewGuid(), _userRepository, cancellationToken);
        return (accessToken, expiresIn);
    }

    public async Task<(string accessToken, string? refreshToken, int expiresIn)> PasswordGrantAsync(
                        string username,
                        string password,
                        string clientId,
                        string clientSecret,
                        IEnumerable<string> scopes,
                        CancellationToken cancellationToken)
    {
        var client = _clientStore.FindClientById(clientId)
            ?? throw new InvalidOperationException("Unknown client");
        if (!client.AllowedGrantTypes.Contains("password"))
            throw new InvalidOperationException($"grant_type 'password' not allowed");
        if (!client.VerifySecret(clientSecret))
            throw new InvalidOperationException("Invalid secret");

        var userId = await _accountsService.ValidateUserCredentials(username, password, cancellationToken);

        if (userId is null)
            throw new InvalidOperationException("Invalid username or password");

        var expiresIn = _tokenService.ExpiryInSeconds;
        var accessToken = await _tokenService.CreateAccessToken(userId.Value, _userRepository, cancellationToken);
        //var refreshToken = await _tokenService.CreateRefreshToken(user.Id, cancellationToken);
        string? refreshToken = null;

        return (accessToken, refreshToken, expiresIn);
    }

    public async Task<bool> AuthorizeSubjectAndUpdateAuthCode(string username, string password, Guid credentialId, CancellationToken cancellationToken)
    {
        var auth = await FindAuthByIdAsync(credentialId, cancellationToken);
        if (auth == null)
            return false;
        var userId = await _accountsService.ValidateUserCredentials(username, password, cancellationToken);
        if (userId is null)
            return false;
        await _experimentalSignInService.LoginForceByIdAsync(userId.Value, cancellationToken);
        auth.SubjectId = userId.Value;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (rt != null) { rt.Revoked = true; await _db.SaveChangesAsync(); }
    }

    // helpers
    private static string CryptoRandomString(int len)
    {
        var bytes = RandomNumberGenerator.GetBytes(len);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool VerifyPkce(string? method, string codeChallenge, string codeVerifier)
    {
        if (string.IsNullOrEmpty(method) || method == "plain")
            return codeChallenge == codeVerifier;

        if (method == "S256")
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var encoded = Base64UrlEncode(hash);
            return encoded == codeChallenge;
        }
        return false;
    }
}
