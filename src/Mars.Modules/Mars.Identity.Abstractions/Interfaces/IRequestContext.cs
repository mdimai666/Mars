using System.Security.Claims;
using Mars.Identity.Abstractions.Dto.Users;

namespace Mars.Identity.Abstractions.Interfaces;

public interface IRequestContext
{
    ClaimsPrincipal Claims { get; }
    string Jwt { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
    HashSet<string>? Roles { get; }

    RequestContextUser? User { get; }
}
