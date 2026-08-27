using Mars.Host.Shared.Dto.Profile;
using Mars.Shared.Contracts.Auth;

namespace Mars.Host.Shared.Dto.Auth;

public static class AuthRequestExtensions
{
    //public static ChangePasswordDto ToQuery(this ChangePasswordRequest request)
    //    => new()
    //    {
    //        UserId = request.UserId,
    //        Password = request.Password,
    //    };

    public static UserForRegistrationQuery ToQuery(this UserForRegistrationRequest request)
        => new()
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

}
