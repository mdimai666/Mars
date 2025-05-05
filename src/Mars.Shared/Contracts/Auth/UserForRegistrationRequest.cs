using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mars.Shared.Resources;

namespace Mars.Shared.Contracts.Auth;

public record UserForRegistrationRequest
{
    [EmailAddress]
    [Required]
    [Display(Name = "E-mail")]
    public required string Email { get; init; }

    [Display(Name = "Пароль")]
    [Category("Security")]
    [Description("Account password")]
    [PasswordPropertyText(true)]
    [Required]
    public required string Password { get; init; }

    [Display(Name = nameof(AppRes.FirstName), ResourceType = typeof(AppRes))]
    public required string? FirstName { get; init; }

    [Display(Name = nameof(AppRes.LastName), ResourceType = typeof(AppRes))]
    public required string? LastName { get; init; }

}
