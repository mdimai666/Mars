using System.ComponentModel.DataAnnotations;
using Mars.Cms.Contracts.MetaFields;
using Mars.Contracts.Resources;

namespace Mars.Identity.Contracts.Users.UserProfiles;

/// <summary>
/// Редактирование собственного профиля. Форма запроса — whitelist:
/// UserName, Email, роли и Type сюда не входят и изменению не подлежат.
/// </summary>
public record UpdateUserProfileRequest
{
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.FirstName), ResourceType = typeof(AppRes))]
    public required string FirstName { get; init; }

    [Display(Name = nameof(AppRes.LastName), ResourceType = typeof(AppRes))]
    public required string? LastName { get; init; }

    [Display(Name = nameof(AppRes.MiddleName), ResourceType = typeof(AppRes))]
    public required string? MiddleName { get; init; }

    [Display(Name = nameof(AppRes.Phone), ResourceType = typeof(AppRes))]
    [RegularExpression(@"^\+?\d{3,12}$", ErrorMessageResourceName = nameof(AppRes.InvalidPhoneNumberError), ErrorMessageResourceType = typeof(AppRes))]
    public required string? PhoneNumber { get; init; }

    [Display(Name = nameof(AppRes.BirthDate), ResourceType = typeof(AppRes))]
    public required DateTime? BirthDate { get; init; }

    [Display(Name = nameof(AppRes.Gender), ResourceType = typeof(AppRes))]
    public required UserGender Gender { get; init; }

    public required string? AvatarUrl { get; init; }

    public required IReadOnlyCollection<UpdateMetaValueRequest> MetaValues { get; init; }
}
