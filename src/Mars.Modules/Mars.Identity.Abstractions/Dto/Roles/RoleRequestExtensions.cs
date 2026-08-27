using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.Roles;

namespace Mars.Host.Shared.Dto.Roles;

public static class RoleRequestExtensions
{
    public static CreateRoleQuery ToQuery(this CreateRoleRequest request)
        => new()
        {
            Id = default,
            Name = request.Name,
        };

    public static UpdateRoleQuery ToQuery(this UpdateRoleRequest request)
        => new()
        {
            Id = request.Id,
            Name = request.Name,
        };

    public static ListRoleQuery ToQuery(this ListRoleQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListRoleQuery ToQuery(this TableRoleQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };
}
