using Mars.Identity.Abstractions.Interfaces;
using Mars.Nodes.Abstractions.Dto;

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
