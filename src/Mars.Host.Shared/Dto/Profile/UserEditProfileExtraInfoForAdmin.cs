using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Roles;

namespace Mars.Host.Shared.Dto.Profile;

public class UserEditProfileExtraInfoForAdmin
{
    //[StringLength(30, MinimumLength = 6, ErrorMessageResourceName = nameof(AppRes.v_range), ErrorMessageResourceType = typeof(AppRes))]
    //[Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    //[Display(Name = "Пароль")]
    //public string Password { get; set; }

    public IEnumerable<Guid> UserRoles { get; set; }

    /// <summary>
    /// Список ролей для выбора
    /// </summary>
    [Display(Name = "Роли")]
    public IEnumerable<RoleSummaryResponse> AvailRoles { get; set; }

    //[Display(Name = "Статус")]
    //public EUserStatus Status { get; set; }

    public UserEditProfileExtraInfoForAdmin()
    {

    }

}
