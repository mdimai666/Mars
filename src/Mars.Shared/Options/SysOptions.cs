using System.ComponentModel.DataAnnotations;
using Mars.Shared.Resources;

namespace Mars.Shared.Options;

public class SysOptions
{
    // General
    [Url]
    [Display(Name = "Адрес сайта")]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    public string SiteUrl { get; set; } = "";

    [Display(Name = "Имя сайта")]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    public string SiteName { get; set; } = "";

    [Display(Name = "Описание сайта")]
    public string SiteDescription { get; set; } = "";

    [EmailAddress]
    [Display(Name = "Email админа")]
    public string AdminEmail { get; set; } = "";

    [Display(Name = "Разрешить самостоятельную регистрацию")]
    public bool AllowUsersSelfRegister { get; set; }
    [Display(Name = "Роль по умолчанию для новых пользователей")]
    public Guid Default_Role { get; set; }

}
