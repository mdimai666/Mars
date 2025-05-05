using System.Security.Claims;

namespace AppFront.Shared.AuthProviders;

public class UserFromClaims
{
    public Guid Id { get; internal set; }
    public string Username { get; internal set; } = "";
    public string Email { get; internal set; } = "";
    public string FirstName { get; internal set; } = "";
    public string LastName { get; internal set; } = "";
    private string SecurityStamp { get; set; } = "";

    public List<string> Roles { get; internal set; } = new();
    public string Role { get; internal set; } = "";

    public bool IsAdmin => Roles.Contains("Admin");
    public bool IsDeveloper => Roles.Contains("Developer");

    public bool IsAuth => Id != Guid.Empty;

    public UserFromClaims()
    {

    }

    public UserFromClaims(ClaimsPrincipal claimsPrincipal)
    {
        Dictionary<string, Action<UserFromClaims, string>> dict = new()
        {
            [ClaimTypes.NameIdentifier] = (user, val) => user.Id = Guid.Parse(val),
            [ClaimTypes.Name] = (user, val) => user.Username = val,
            [ClaimTypes.Email] = (user, val) => user.Email = val,
            ["AspNet.Identity.SecurityStamp"] = (user, val) => user.SecurityStamp = val,
            [ClaimTypes.GivenName] = (user, val) => user.FirstName = val,
            [ClaimTypes.Surname] = (user, val) => user.LastName = val,

        };

        foreach (var claim in claimsPrincipal.Claims)
        {
            if (dict.ContainsKey(claim.Type))
            {
                var action = dict[claim.Type];
                action(this, claim.Value);
            }
        }

        List<string> roles = new();

        foreach (var claim in claimsPrincipal.Claims)
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
        Roles = roles;
        Role = Roles.First();

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
        else
            _initials = Email.Substring(0, 2).ToUpper();
        return _initials;
    }
}
