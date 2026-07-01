using System.Security.Claims;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Interfaces;

namespace StandNodesApp.Mocks;

internal class RequestContextMock : IRequestContext
{
    public ClaimsPrincipal Claims { get; }
    public string Jwt { get; } = "";
    public string UserName { get; } = "Admin";
    public bool IsAuthenticated { get; } = true;
    public HashSet<string>? Roles { get; } = ["Admin"];
    public RequestContextUser? User { get; }

    public RequestContextMock()
    {
        Claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.Name, UserName),
            new(ClaimTypes.Role, "Admin")
        }));

        User = CreateMockUser();
    }

    public RequestContextUser CreateMockUser()
    {
        return new RequestContextUser
        {
            Id = Guid.NewGuid(),
            UserName = UserName,
            Email = "example@mail.ru",
            FirstName = "Admin",
            LastName = "A",
            MiddleName = null,
            Gender = Mars.Shared.Contracts.Users.UserGender.Male,
            BirthDate = null,
            PhoneNumber = null,
            AvatarUrl = null,
            Roles = ["Admin"],
            //SecurityStamp = Guid.NewGuid().ToString()
        };
    }
}
