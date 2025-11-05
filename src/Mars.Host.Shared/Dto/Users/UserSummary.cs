using Mars.Core.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Mars.Host.Shared.Dto.Users;

public record UserSummary
{
    public required Guid Id { get; init; }

    [PersonalData]
    public required string FirstName { get; init; }

    [PersonalData]
    public required string LastName { get; init; }

    [PersonalData]
    public required string? MiddleName { get; init; }

    [PersonalData]
    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    public required string? AvatarUrl { get; init; }
}
