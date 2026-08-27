using Mars.Shared.Common;

namespace Mars.Host.Shared.Dto.Users;

public record ListUserQuery : BasicListQuery
{
    public IReadOnlyCollection<string>? Roles { get; init; }
}
