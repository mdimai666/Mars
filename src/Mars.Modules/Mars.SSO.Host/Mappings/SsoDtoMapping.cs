using Mars.Identity.Abstractions.Dto.SSO;
using Mars.SSO.Contracts.Dto;
using Mars.SSO.Contracts.Dto;

namespace Mars.SSO.Host.Mappings;

internal static class SsoDtoMapping
{
    internal static SsoProviderInfo ToInfo(this SsoProviderDescriptor descriptor)
        => new()
        {
            Driver = descriptor.Driver,
            DisplayName = descriptor.DisplayName,
            ProviderSlug = descriptor.Name
        };

    internal static SsoUserInfoResponse ToResponse(this SsoUserInfo entity)
        => new()
        {
            InternalId = entity.InternalId,
            ExternalId = entity.ExternalId,
            Email = entity.Email,
            Name = entity.Name,
            Provider = entity.Provider,
            AccessToken = entity.AccessToken,
            UserPrimaryInfo = entity.UserPrimaryInfo
        };
}
