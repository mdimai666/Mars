using Mars.Host.Shared.Dto.Auth;
using Mars.Shared.Contracts.Auth;

namespace Mars.Host.Shared.Mappings.Accounts;

public static class AuthMapping
{
    public static AuthResultResponse ToResponse(this AuthResultDto entity)
        => new()
        {
            ErrorMessage = entity.ErrorMessage,
            ExpiresIn = entity.ExpiresIn,
            RefreshToken = entity.RefreshToken,
            Token = entity.Token,
        };

    public static RegistrationResultResponse ToResponse(this RegistrationResponseDto entity)
        => new()
        {
            Errors = entity.Errors,
            IsSuccessfulRegistration = entity.IsSuccessfulRegistration,
            Code = entity.Code,
        };
}
