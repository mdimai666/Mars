using Mars.Core.Extensions;

namespace Mars.Shared.Contracts.Users;

public record UserSummaryResponse
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string? MiddleName { get; init; }
    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());
    public required string? AvatarUrl { get; init; }
}
