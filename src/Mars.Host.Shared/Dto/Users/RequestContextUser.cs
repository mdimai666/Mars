using Mars.Core.Extensions;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Shared.Dto.Users;

public record RequestContextUser
{
    public required Guid Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string? MiddleName { get; init; }

    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    public required string? Email { get; init; }
    public required string? PhoneNumber { get; init; }
    public required DateTime? BirthDate { get; init; }

    public required UserGender Gender { get; init; }

    public required string UserName { get; init; }

}
