using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Shared.Dto.Users;

public record UserEditDetail : UserSummary
{
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }

    [PersonalData]
    public required string? PhoneNumber { get; init; }

    [PersonalData]
    public required string? Email { get; init; }

    public required string UserName { get; init; }
    //public required string About { get; init; }

    [PersonalData]
    public required DateTime? BirthDate { get; init; }

    public required UserGender Gender { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyCollection<MetaValueDetailDto> MetaValues { get; init; }

    public required UserTypeDetail UserTypeDetail { get; init; }
}
