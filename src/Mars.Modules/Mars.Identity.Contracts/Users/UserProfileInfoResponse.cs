using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.Users;

public class UserProfileInfoResponse : UserEditProfileResponse
{
    [Display(Name = "Роли")]
    public required IReadOnlyCollection<string> Roles { get; set; }

    [Display(Name = "Количество комментариев")]
    public required int CommentCount { get; init; }

}
