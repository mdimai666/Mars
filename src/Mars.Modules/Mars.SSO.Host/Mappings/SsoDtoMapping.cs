using Mars.Host.Shared.Dto.SSO;
using Mars.Host.Shared.SSO.Dto;
using Mars.Shared.Contracts.SSO;

namespace Mars.SSO.Mappings;

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
