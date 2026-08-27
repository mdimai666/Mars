
using System.ComponentModel.DataAnnotations;

namespace Mars.Identity.Abstractions.Dto.Auth;

public class AuthCreditionalsDto
{

    [Required(ErrorMessage = "Заполните Логин/Почту")]
    [Display(Name = "Логин")]
    public required string Login { get; set; }
    [Required(ErrorMessage = "Заполните Пароль")]
    [Display(Name = "Пароль")]
    public required string Password { get; set; }
}
