using System.ComponentModel.DataAnnotations;
using Mars.Contracts.Resources;

namespace Mars.Admin.SiteSpace.Pages.RoleViews;

public class CreateRoleModel
{
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [MinLength(2)]
    [Display(Name = nameof(AppRes.Name), ResourceType = typeof(AppRes))]
    public string Name { get; set; } = "";
}
