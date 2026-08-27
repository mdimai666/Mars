namespace Mars.Identity.Abstractions.Services;

public interface IAccountsService
{
    Task<Guid?> ValidateUserCredentials(string username, string password, CancellationToken cancellationToken);
}
