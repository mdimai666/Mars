using Mars.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Shared.Dto.Users;

public record AuthorizedUserInformationDto : UserSummary
{
    [PersonalData]
    public required string? PhoneNumber { get; init; }

    [PersonalData]
    public required string? Email { get; init; }

    public required string UserName { get; init; }

    [PersonalData]
    public required DateTime? BirthDate { get; init; }

    public required UserGender Gender { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public required string SecurityStamp { get; init; }
}
