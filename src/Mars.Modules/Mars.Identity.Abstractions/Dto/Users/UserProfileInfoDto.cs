using System.ComponentModel.DataAnnotations;

namespace Mars.Host.Shared.Dto.Users;

public class UserProfileInfoDto : UserEditProfileDto
{
    [Display(Name = "Роли")]
    public required IEnumerable<string> Roles { get; init; }

    [Display(Name = "Количество комментариев")]
    public required int CommentCount { get; init; }
}
