using System.ComponentModel.DataAnnotations;

namespace Mars.Shared.Contracts.Auth;

public record AuthCreditionalsRequest
{

    [Required(ErrorMessage = "Заполните Логин/Почту")]
    [Display(Name = "Логин")]
    public required string Login { get; init; }
    [Required(ErrorMessage = "Заполните Пароль")]
    [Display(Name = "Пароль")]
    public required string Password { get; init; }
}
