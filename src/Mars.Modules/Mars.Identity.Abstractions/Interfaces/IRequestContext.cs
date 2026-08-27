using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;

namespace Mars.Host.Shared.Interfaces;

public interface IRequestContext
{
    ClaimsPrincipal Claims { get; }
    string Jwt { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
    HashSet<string>? Roles { get; }

    RequestContextUser? User { get; }
}
