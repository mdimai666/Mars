using System.ComponentModel.DataAnnotations;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.Users;

namespace Mars.Host.Shared.Dto.Users;

public record CreateUserQuery
{
    public Guid? Id { get; set; }
    public required string UserName { get; init; }
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }

    [EmailAddress]
    public string? Email { get; init; }
    public required string Password { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public DateTime? BirthDate { get; init; }
    public UserGender Gender { get; init; }
    public string? PhoneNumber { get; init; }

    public required string Type { get; init; }
    public required IReadOnlyCollection<ModifyMetaValueDetailQuery> MetaValues { get; init; }
}
