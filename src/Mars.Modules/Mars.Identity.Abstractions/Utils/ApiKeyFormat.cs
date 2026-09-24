using System.Security.Cryptography;
using System.Text;

namespace Mars.Identity.Abstractions.Utils;

/// <summary>
/// Формат ключа: <c>mars_{keyId}.{secret}</c>. В БД хранится только SHA-256 хэш secret.
/// </summary>
public static class ApiKeyFormat
{
    public const string Prefix = "mars_";

    public static string GenerateSecret()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Build(Guid keyId, string secret) => $"{Prefix}{keyId}.{secret}";

    public static bool TryParse(string? key, out Guid keyId, out string secret)
    {
        keyId = Guid.Empty;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        var separatorIndex = key.IndexOf('.', Prefix.Length);
        if (separatorIndex < 0 || separatorIndex == key.Length - 1)
            return false;

        var idPart = key[Prefix.Length..separatorIndex];
        if (!Guid.TryParse(idPart, out keyId))
            return false;

        secret = key[(separatorIndex + 1)..];
        return secret.Length > 0;
    }

    public static string HashSecret(string secret)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    public static bool SecretMatchesHash(string secret, string keyHash)
    {
        var secretHashBytes = Convert.FromBase64String(HashSecret(secret));
        var storedHashBytes = Convert.FromBase64String(keyHash);
        return CryptographicOperations.FixedTimeEquals(secretHashBytes, storedHashBytes);
    }

    public static string DisplayPrefix(Guid keyId) => $"{Prefix}{keyId.ToString()[..8]}…";
}
