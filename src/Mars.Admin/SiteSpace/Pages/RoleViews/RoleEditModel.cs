using System.ComponentModel.DataAnnotations;
using Mars.Contracts.Resources;
using Mars.Identity.Contracts.ViewModels;

namespace Mars.Admin.SiteSpace.Pages.RoleViews;

public class RoleEditModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [MinLength(2)]
    [Display(Name = nameof(AppRes.Name), ResourceType = typeof(AppRes))]
    public string Name { get; set; } = "";

    /// <summary>Локализованное название (с сервера, EditRolesViewModel).</summary>
    public string Title { get; set; } = "";

    /// <summary>Группы прав текущей роли — тот же массив, что в матрице страницы.</summary>
    public RoleCapGroupCheckable[] Groups { get; set; } = [];

    /// <summary>
    /// Admin — суперпользователь: handler обходит проверку прав, поэтому матрицу показываем
    /// полностью включённой и блокируем редактирование (защита от локаута).
    /// </summary>
    public bool IsSuperuser { get; set; }
}
