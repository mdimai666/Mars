using Microsoft.IdentityModel.Tokens;

namespace Mars.Host.Shared.Services;

public interface IKeyMaterialService
{
    string KeyId { get; }
    SecurityKey SigningKey { get; }

    SigningCredentials GetSigningCredentials();
    JsonWebKey GetJwk();
    string GetJwksJson();
}
