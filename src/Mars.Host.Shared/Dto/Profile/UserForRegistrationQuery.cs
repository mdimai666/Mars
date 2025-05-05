using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mars.Shared.Resources;

namespace Mars.Host.Shared.Dto.Profile;

public class UserForRegistrationQuery
{
    [EmailAddress]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = "E-mail")]
    public required string Email { get; init; }

    [Display(Name = "Пароль")]
    [Category("Security")]
    [Description("Account password")]
    [PasswordPropertyText(true)]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    public required string Password { get; init; }

    [Required]
    [Display(Name = nameof(AppRes.FirstName), ResourceType = typeof(AppRes))]
    public required string? FirstName { get; init; }

    [Display(Name = nameof(AppRes.LastName), ResourceType = typeof(AppRes))]
    public required string? LastName { get; init; }

}
