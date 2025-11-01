namespace Mars.Host.Shared.Services;

public interface IAccountsService
{
    Task<Guid?> ValidateUserCredentials(string username, string password, CancellationToken cancellationToken);
}
