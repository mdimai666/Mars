using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Contracts.Auth;

public record AuthCredentialsRequest
{

    [Required(ErrorMessage = "Заполните Логин/Почту")]
    [Display(Name = "Логин")]
    public required string Login { get; init; }
    [Required(ErrorMessage = "Заполните Пароль")]
    [Display(Name = "Пароль")]
    public required string Password { get; init; }
}
