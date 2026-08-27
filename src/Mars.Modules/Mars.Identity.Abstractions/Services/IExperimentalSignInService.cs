namespace Mars.Host.Shared.Services;

public interface IExperimentalSignInService
{
    Task LoginForceByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task LoginForceByNameIdentifierAsync(string providerName, string nameIdentifier, CancellationToken cancellationToken);
}
