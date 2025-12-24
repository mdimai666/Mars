using Mars.Host.Shared.Interfaces;
using Mars.Nodes.Host.Shared.Dto;

namespace Mars.Nodes.Host.Mappings;

internal static class IRequestContextExtensions
{
    internal static RequestUserInfo ToRequestUserInfo(this IRequestContext requestContext)
        => new()
        {
            IsAuthenticated = requestContext.IsAuthenticated,
            UserId = requestContext.User?.Id,
            UserName = requestContext.User?.UserName,
        };
}
