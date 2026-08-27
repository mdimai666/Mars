using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Services;

public class ExperimentalSignInService(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager) : IExperimentalSignInService
{
    public async Task LoginForceByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new InvalidOperationException("user not found");

        await signInManager.SignInAsync(user, false);
    }

    public async Task LoginForceByNameIdentifierAsync(string providerName, string nameIdentifier, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByLoginAsync(providerName, nameIdentifier) ?? throw new InvalidOperationException("user not found");

        await signInManager.SignInAsync(user, false);
    }
}
