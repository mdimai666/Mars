using Mars.Identity.Abstractions.Dto.Profile;
using Mars.Identity.Contracts.Auth;

namespace Mars.Identity.Abstractions.Dto.Auth;

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
