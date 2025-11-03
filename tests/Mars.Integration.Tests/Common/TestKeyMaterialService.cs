using System.Security.Cryptography;
using System.Text.Json;
using Mars.Host.Shared.Services;
using Microsoft.IdentityModel.Tokens;

namespace Mars.Integration.Tests.Common;

public class TestKeyMaterialService : IKeyMaterialService
{
    private readonly RSA _rsa;

    public SecurityKey SigningKey { get; }
    private const string _keyId = "main-jwt-key";
    public string KeyId => _keyId;

    public TestKeyMaterialService()
    {

        _rsa = RSA.Create(2048);
        SigningKey = new RsaSecurityKey(_rsa)
        {
            KeyId = _keyId
        };
    }

    public SigningCredentials GetSigningCredentials() =>
        new(SigningKey, SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = true }
        };

    public JsonWebKey GetJwk()
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey((RsaSecurityKey)SigningKey);

        // Обычно KeyId и алгоритм RS256 нужны явно
        jwk.Kid = KeyId;
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        jwk.Use = "sig";

        return jwk;
    }

    string? _getJwksJson;

    /// <summary>
    /// Получение JWKS, совместимого с jwt.io
    /// </summary>
    public string GetJwksJson()
    {
        if (_getJwksJson != null) return _getJwksJson;

        var rsaKey = (RsaSecurityKey)SigningKey;

        // Параметры RSA в Base64Url
        var e = Base64UrlEncoder.Encode(rsaKey.Rsa.ExportParameters(false).Exponent);
        var n = Base64UrlEncoder.Encode(rsaKey.Rsa.ExportParameters(false).Modulus);

        var publicKeyBytes = _rsa.ExportSubjectPublicKeyInfo();

        // SHA-1 thumbprint (x5t)
        using var sha1 = SHA1.Create();
        var x5tBytes = sha1.ComputeHash(publicKeyBytes);
        var x5t = Base64UrlEncoder.Encode(x5tBytes);

        // SHA-256 thumbprint (x5t#S256)
        using var sha256 = SHA256.Create();
        var x5tS256Bytes = sha256.ComputeHash(publicKeyBytes);
        var x5tS256 = Base64UrlEncoder.Encode(x5tS256Bytes);

        // x5c как Base64 сертификат публичного ключа
        var x5c = Convert.ToBase64String(publicKeyBytes);

        var jwk = new Dictionary<string, object>
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["kid"] = KeyId,
            ["alg"] = "RS256",
            ["n"] = n,
            ["e"] = e,
            ["x5c"] = new[] { x5c },
            ["x5t"] = x5t,
            ["x5t#S256"] = x5tS256
        };

        var jwks = new { keys = new[] { jwk } };

        return _getJwksJson = JsonSerializer.Serialize(jwks, new JsonSerializerOptions { WriteIndented = true });
    }
}
