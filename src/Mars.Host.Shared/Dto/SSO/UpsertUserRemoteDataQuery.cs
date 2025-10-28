using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Shared.Dto.SSO;

public record UpsertUserRemoteDataQuery
{
    /// <summary>
    /// Это уникальный идентификатор пользователя у конкретного провайдера.
    /// Например:
    /// В Google это может быть внутренний ID Google-профиля.
    /// В Keycloak — GUID пользователя из Keycloak.
    /// В Facebook — ID из Facebook Graph API.
    /// </summary>
    public required string ExternalKey { get; init; }
    public required string PreferredUserName { get; init; }
    public required string FirstName { get; init; }
    public required string? LastName { get; init; }
    public required string? MiddleName { get; init; }

    [EmailAddress]
    public required string? Email { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; } = [];

    public DateTime? BirthDate { get; init; }
    public UserGender Gender { get; init; }
    public string? PhoneNumber { get; init; }
    public required SsoProviderInfo Prodvider { get; init; }

}
