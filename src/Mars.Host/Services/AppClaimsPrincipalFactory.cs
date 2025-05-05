using System.Security.Claims;
using Mars.Host.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Mars.Host.Services;

/// <summary>
/// Это надо потому как я переопределил стандартного IdentityUser В User
/// </summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<UserEntity, RoleEntity>
{
    public AppClaimsPrincipalFactory(
        UserManager<UserEntity> userManager
        , RoleManager<RoleEntity> roleManager
        , IOptions<IdentityOptions> optionsAccessor)
    : base(userManager, roleManager, optionsAccessor)
    {

    }

    public async override Task<ClaimsPrincipal> CreateAsync(UserEntity user)
    {
        var principal = await base.CreateAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                new Claim(ClaimTypes.GivenName, user.FirstName)
            });
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                 new Claim(ClaimTypes.Surname, user.LastName),
            });
        }

        return principal;
    }
}
