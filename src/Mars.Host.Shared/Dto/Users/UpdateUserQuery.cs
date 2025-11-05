using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Shared.Dto.Users;

public record UpdateUserQuery
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; }
    public required string FirstName { get; init; }
    public required string? LastName { get; init; }
    public required string? MiddleName { get; init; }

    [EmailAddress]
    public required string? Email { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; } = [];

    public DateTime? BirthDate { get; init; }
    public UserGender Gender { get; init; }
    public string? PhoneNumber { get; init; }
    public required string? AvatarUrl { get; init; }

    public required string Type { get; init; }
    public required IReadOnlyCollection<ModifyMetaValueDetailQuery> MetaValues { get; init; }
}
