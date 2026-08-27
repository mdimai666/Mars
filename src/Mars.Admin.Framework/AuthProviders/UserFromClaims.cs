using System.Security.Claims;
using AppFront.Shared.Extensions;
using Mars.Shared.Contracts.SSO;
using Mars.Shared.ViewModels;

namespace AppFront.Shared.AuthProviders;

public class UserFromClaims
{
    public Guid Id { get; private set; }
    public string? ExternalId { get; private set; }
    public string Username { get; private set; } = "";
    public string? Email { get; private set; }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    private string SecurityStamp { get; set; } = "";

    public string? AvatarUrl { get; private set; }
    public HashSet<string> Roles { get; private set; } = [];

    public bool IsAdmin => Roles.Contains("Admin");
    public bool IsDeveloper => Roles.Contains("Developer");

    public bool IsAuth => Id != Guid.Empty;

    public UserFromClaims()
    {

    }

    public UserFromClaims(ClaimsPrincipal principal, Guid? internalMarsId = null)
    {
        Id = internalMarsId != null ? internalMarsId.Value : Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        Username = principal.FindFirstValue(ClaimTypes.Name) ?? "";
        Email = principal.FindFirstValue(ClaimTypes.Email);
        FirstName = principal.FindFirstValue(ClaimTypes.GivenName) ?? "";
        LastName = principal.FindFirstValue(ClaimTypes.Surname) ?? "";
        SecurityStamp = principal.FindFirstValue("AspNet.Identity.SecurityStamp") ?? "";
        AvatarUrl = principal.FindFirstValue("picture");

        ExternalId = internalMarsId == null ? null : principal.FindFirstValue(ClaimTypes.NameIdentifier);

        List<string> roles = [];

        foreach (var claim in principal.Claims)
        {
            if (claim.Type == ClaimTypes.Role)
            {
                roles.Add(claim.Value);
            }
        }

        //if (roles.Count == 0) throw new Exception("Role claim not exist");
        if (roles.Count == 0)
        {
            roles.Add("User");
        }

        roles.Sort();
        Roles = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

    }

    public UserFromClaims(UserPrimaryInfo userPrimaryInfo, string? externalId)
    {
        Id = userPrimaryInfo.Id;
        ExternalId = externalId;
        Username = userPrimaryInfo.Username;
        Email = userPrimaryInfo.Email;
        FirstName = userPrimaryInfo.FirstName;
        LastName = userPrimaryInfo.LastName;
        SecurityStamp = "";
        Roles = userPrimaryInfo.Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        AvatarUrl = userPrimaryInfo.AvatarUrl;
    }

    public UserFromClaims(SsoUserInfoResponse ssoUserInfo)
    {
        Id = ssoUserInfo.InternalId;
        ExternalId = ssoUserInfo.ExternalId;
        Username = ssoUserInfo.UserPrimaryInfo.Username;
        Email = ssoUserInfo.UserPrimaryInfo.Email;
        FirstName = ssoUserInfo.UserPrimaryInfo.FirstName;
        LastName = ssoUserInfo.UserPrimaryInfo.LastName;
        SecurityStamp = "";
        Roles = ssoUserInfo.UserPrimaryInfo.Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        AvatarUrl = ssoUserInfo.UserPrimaryInfo.AvatarUrl;
    }

    string _initials = null!;

    public string Initials()
    {
        if (!IsAuth) return "An";
        if (_initials != null) return _initials;
        if (!string.IsNullOrEmpty(LastName) && !string.IsNullOrEmpty(FirstName))
            _initials = $"{LastName[0]}{FirstName[0]}".ToUpper();
        else if (!string.IsNullOrEmpty(LastName))
            _initials = $"{LastName[0]}".ToUpper();
        else if (!string.IsNullOrEmpty(FirstName))
            _initials = $"{FirstName[0]}".ToUpper();
        else if (!string.IsNullOrEmpty(Email))
            _initials = Email.Substring(0, 2).ToUpper();
        else
            _initials = "An";
        return _initials;
    }
}
