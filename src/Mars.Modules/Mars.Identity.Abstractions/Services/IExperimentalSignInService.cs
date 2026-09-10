namespace Mars.Identity.Abstractions.Services;

public interface IExperimentalSignInService
{
    Task LoginForceByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task LoginForceByNameIdentifierAsync(string providerName, string nameIdentifier, CancellationToken cancellationToken);
}
