using System.ComponentModel.DataAnnotations;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Admin.SiteSpace.Pages.UserTypeViews;
using Mars.Contracts.Resources;
using Mars.Core.Extensions;
using Mars.Core.Interfaces;
using Mars.Forms.Contracts;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.Users.UserProfiles;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.SiteSpace.Pages.UserViews;

/// <summary>
/// Модель собственного профиля (страницы /Profile и /Profile/Edit).
/// Редактируемые поля — whitelist серверного <see cref="UpdateUserProfileRequest"/>;
/// UserName/Email/Roles/Type — только чтение.
/// </summary>
public class ProfileEditModel : IHasId
{
    public Guid Id { get; set; }

    //----- Аккаунт (read-only) -----
    public string UserName { get; init; } = "";
    public string Email { get; init; } = "";
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public string Type { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }

    //----- Личные данные (редактируемые) -----
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.FirstName), ResourceType = typeof(AppRes))]
    public string FirstName { get; set; } = "";

    [Display(Name = nameof(AppRes.LastName), ResourceType = typeof(AppRes))]
    public string LastName { get; set; } = "";

    [Display(Name = nameof(AppRes.MiddleName), ResourceType = typeof(AppRes))]
    public string MiddleName { get; set; } = "";

    [Display(Name = nameof(AppRes.Phone), ResourceType = typeof(AppRes))]
    [RegularExpression(@"^\+?\d{3,12}$", ErrorMessageResourceName = nameof(AppRes.InvalidPhoneNumberError), ErrorMessageResourceType = typeof(AppRes))]
    public string PhoneNumber { get; set; } = "";

    [Display(Name = nameof(AppRes.BirthDate), ResourceType = typeof(AppRes))]
    public DateTime? BirthDate { get; set; }

    [Display(Name = nameof(AppRes.Gender), ResourceType = typeof(AppRes))]
    public UserGender Gender { get; set; }

    public string AvatarUrl { get; set; } = "";

    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());

    //----- Метаполя -----
    public List<MetaValueEditModel> MetaValues { get; set; } = [];

    /// <summary>Дерево формы метаполей владельца (общий слой Mars.Forms)</summary>
    public FormDefinition? Form { get; set; }
    public UserTypeEditModel UserType { get; init; } = new();

    public static async Task<ProfileEditModel> GetAction(IMarsWebApiClient client)
    {
        var vm = await client.Account.GetProfileEditModel();
        return FromViewModel(vm);
    }

    public static async Task<ProfileEditModel> SaveAction(IMarsWebApiClient client, ProfileEditModel model, bool isNew)
    {
        await client.Account.UpdateProfile(model.ToUpdateRequest());
        return model;
    }

    public UpdateUserProfileRequest ToUpdateRequest()
        => new()
        {
            FirstName = FirstName,
            LastName = LastName.AsNullIfEmpty(),
            MiddleName = MiddleName.AsNullIfEmpty(),
            PhoneNumber = PhoneNumber.AsNullIfEmpty(),
            BirthDate = BirthDate,
            Gender = Gender,
            AvatarUrl = AvatarUrl.AsNullIfEmpty(),
            MetaValues = MetaValues.Select(s => s.ToUpdateRequest()).ToList(),
        };

    public static ProfileEditModel FromViewModel(UserProfileEditViewModel vm)
    {
        var user = vm.User;
        return new()
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email ?? "",
            Roles = user.Roles,
            Type = user.Type,
            CreatedAt = user.CreatedAt,

            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName ?? "",
            PhoneNumber = user.Phone ?? "",
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            AvatarUrl = user.AvatarUrl ?? "",

            MetaValues = user.MetaValues.Select(MetaValueEditModel.ToModel).ToList(),
            Form = vm.Form,
            UserType = UserTypeEditModel.ToModel(vm.UserType, []),
        };
    }
}
