using Mars.Shared.Contracts.MetaFields;

namespace Mars.Shared.Contracts.Users;

public class UserDetailResponse : UserSummaryResponse
{
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? ModifiedAt { get; init; }
    public required string? PhoneNumber { get; init; }
    public required string? Email { get; init; }
    public required string UserName { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }

    public required DateTime? BirthDate { get; init; }
    public required UserGender Gender { get; init; }
    public required string Type { get; init; }
    public required IReadOnlyCollection<MetaValueResponse> MetaValues { get; init; }
}
