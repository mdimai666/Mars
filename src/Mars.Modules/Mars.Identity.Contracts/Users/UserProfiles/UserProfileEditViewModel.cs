using Mars.Forms.Contracts;
using Mars.Identity.Contracts.UserTypes;

namespace Mars.Identity.Contracts.Users.UserProfiles;

/// <summary>
/// Модель редактирования собственного профиля: без AvailRoles —
/// роли пользователя доступны только админу через <see cref="UserEditViewModel"/>.
/// </summary>
public record UserProfileEditViewModel
{
    public required UserEditResponse User { get; init; }
    public required UserTypeDetailResponse UserType { get; init; }

    /// <summary>Дерево формы метаполей пользователя (общий слой Mars.Forms)</summary>
    public FormDefinition? Form { get; init; }
}
