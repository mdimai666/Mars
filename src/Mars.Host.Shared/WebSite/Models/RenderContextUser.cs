using Mars.Host.Shared.Dto.Users;
using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Shared.WebSite.Models;

public class RenderContextUser
{
    public Guid Id { get; init; }

    [PersonalData]
    public string FirstName => Detail.UserName;

    [PersonalData]
    public string LastName => Detail.LastName;

    [PersonalData]
    public string? MiddleName => Detail.MiddleName;

    [PersonalData]
    public string FullName => Detail.FullName;

    [PersonalData]
    public string? Email => Detail.Email;

    public string UserName => Detail.UserName;

    public UserDetail Detail { get; init; }

    public IReadOnlyCollection<string> Roles => Detail.Roles;

    public RenderContextUser(UserDetail userDetail)
    {
        Detail = userDetail;
    }
}
