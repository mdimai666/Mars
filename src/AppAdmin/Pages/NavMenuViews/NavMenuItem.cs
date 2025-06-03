using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mars.Shared.Contracts.NavMenus;

namespace AppAdmin.Pages.NavMenuViews;

public partial class EditNavMenuPage
{
    internal class NavMenuItem
    {
        [Display(Name = "#")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Display(Name = "Родитель")]
        public Guid ParentId { get; set; }

        [Display(Name = "Название")]
        public string Title { get; set; } = "";

        [Display(Name = "Ссылка")]
        public string Url { get; set; } = "";

        [Display(Name = "Иконка")]
        public string? Icon { get; set; }

        [Display(Name = "Роли")]
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Не для ролей
        /// </summary>
        [Display(Name = "Не для ролей")]
        public bool RolesInverse { get; set; }

        [Display(Name = "Class")]
        public string Class { get; set; } = "";

        [Display(Name = "Style")]
        public string Style { get; set; } = "";

        [Display(Name = "Открывать в новой вкладке")]
        public bool OpenInNewTab { get; set; }

        [Display(Name = "Отключен")]
        public bool Disabled { get; set; }

        [Display(Name = "Заголовок")]
        public bool IsHeader { get; set; }

        [Display(Name = "Разделитель")]
        public bool IsDivider { get; set; }

        //extra

        public IEnumerable<string> SetRoles { get => Roles; set => Roles = value.ToList(); }


        public IEnumerable<NavMenuItem> GetItems(NavMenu nav)
        {
            return nav.MenuItems.Where(x => x.ParentId == Id);
        }

        public static NavMenuItem Copy(NavMenuItem item)
        {
            return new NavMenuItem
            {
                Id = Guid.NewGuid(),
                Title = item.Title,
                Url = item.Url,
                Class = item.Class,
                Disabled = item.Disabled,
                Style = item.Style,
                OpenInNewTab = item.OpenInNewTab,
                Icon = item.Icon,
                ParentId = item.ParentId,
                Roles = item.Roles,
                RolesInverse = item.RolesInverse,
            };
        }

        [NotMapped]
        public bool IsFontIcon => !(Icon?.Contains('/') ?? true);

        public CreateNavMenuItemRequest ToCreateRequest()
            => new()
            {
                Id = Id,
                Title = Title,
                Url = Url,
                Class = Class,
                Disabled = Disabled,
                Style = Style,
                Icon = Icon,
                ParentId = ParentId,
                Roles = Roles,
                RolesInverse = RolesInverse,
                IsDivider = IsDivider,
                IsHeader = IsHeader,
                OpenInNewTab = OpenInNewTab
            };

        public UpdateNavMenuItemRequest ToUpdateRequest()
            => new()
            {
                Id = Id,
                Title = Title,
                Url = Url,
                Class = Class,
                Disabled = Disabled,
                Style = Style,
                Icon = Icon,
                ParentId = ParentId,
                Roles = Roles,
                RolesInverse = RolesInverse,
                IsDivider = IsDivider,
                IsHeader = IsHeader,
                OpenInNewTab = OpenInNewTab
            };

        public static NavMenuItem ToModel(NavMenuItemResponse response)
            => new()
            {
                Id = response.Id,
                Title = response.Title,
                Url = response.Url,
                Class = response.Class,
                Disabled = response.Disabled,
                Style = response.Style,
                Icon = response.Icon,
                ParentId = response.ParentId,
                Roles = response.Roles.ToList(),
                RolesInverse = response.RolesInverse,
                IsDivider = response.IsDivider,
                IsHeader = response.IsHeader,
                OpenInNewTab = response.OpenInNewTab,
            };

    }
}
