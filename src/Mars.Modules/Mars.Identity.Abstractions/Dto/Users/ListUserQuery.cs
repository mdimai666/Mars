using Mars.Contracts.Common;

namespace Mars.Identity.Abstractions.Dto.Users;

public record ListUserQuery : BasicListQuery
{
    public IReadOnlyCollection<string>? Roles { get; init; }
}
