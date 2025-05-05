using System.Security.Cryptography;
using System.Text;

namespace Mars.Host.Services.MarsSSOClient;

public class OpenIDAuthAccessCodeData
{
    public required Guid UserId { get; set; }
    public required string OpenIDClientId { get; set; }
    public required Guid Salt { get; set; }
    public DateTime Created { get; init; } = DateTime.Now;

    public required OpenIDAuthForm Form { get; set; }

    public static (string, OpenIDAuthAccessCodeData) Generate(string realm, string client_id, Guid userId, OpenIDAuthForm form)
    {
        Guid realmGuid = StringHashToGuid(realm);
        Guid clientIdGuid = StringHashToGuid(client_id);
        Guid salt = Guid.NewGuid();

        return ($"{realmGuid}.{clientIdGuid}.{salt}", new OpenIDAuthAccessCodeData
        {
            OpenIDClientId = client_id,
            Salt = salt,
            UserId = userId,
            Form = form
        });
    }

    static Guid StringHashToGuid(string input)
    {
        using MD5 md5 = MD5.Create();

        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}