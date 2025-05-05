using System.Security.Claims;
using System.Text.Json;

namespace AppFront.Shared.Features;

public static class JwtParser
{
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        List<Claim> claims = new();
        var payload = jwt.Split('.')[1];

        payload = payload.Replace('_', '/').Replace('-', '+');

        var jsonBytes = ParseBase64WithoutPadding(payload);//фикс для кириллицы

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)!;

        ExtractRolesFromJWT(claims, keyValuePairs);

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));

        return claims;
    }

    public static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    public static void ExtractRolesFromJWT(List<Claim> claims, Dictionary<string, object> keyValuePairs)
    {
        if (keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles))
        {
            var parsedRoles = roles.ToString()!.Trim().TrimStart('[').TrimEnd(']').Split(',');

            if (parsedRoles.Length > 1)
            {
                foreach (var parsedRole in parsedRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, parsedRole.Trim('"')));
                }
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, parsedRoles[0]));
            }

            keyValuePairs.Remove(ClaimTypes.Role);
        }
    }
}
