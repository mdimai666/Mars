using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Mars.Core.Exceptions;
using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.Models.Interfaces;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppAdmin.Pages.NavMenuViews;

public partial class EditNavMenuPage
{
    internal class NavMenu : IBasicEntity
    {
        [Display(Name = nameof(AppRes.Id), ResourceType = typeof(AppRes))]
        public Guid Id { get; set; }

        [Display(Name = nameof(AppRes.CreationDate), ResourceType = typeof(AppRes))]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = nameof(AppRes.DateModified), ResourceType = typeof(AppRes))]
        public DateTimeOffset? ModifiedAt { get; set; }


        [Required]
        [Display(Name = nameof(AppRes.Title), ResourceType = typeof(AppRes))]
        public string Title { get; set; } = "";

        [Required]
        [Display(Name = nameof(AppRes.Slug), ResourceType = typeof(AppRes))]
        public virtual string Slug { get; set; } = Guid.NewGuid().ToString();


        [Comment("Элементы")]
        public IEnumerable<NavMenuItem> MenuItems { get; set; } = new ObservableCollection<NavMenuItem>();

        [Display(Name = nameof(AppRes.CssClass), ResourceType = typeof(AppRes), Description = "например: px-1 mt-2 text-accent")]
        public string Class { get; set; } = "";

        [Display(Name = nameof(AppRes.CssStyle), ResourceType = typeof(AppRes))]
        public string Style { get; set; } = "";

        [Display(Name = nameof(AppRes.Roles), ResourceType = typeof(AppRes))]
        public List<string> Roles { get; set; } = new();

        //[NotMapped]
        //[JsonIgnore, Newtonsoft.Json.JsonIgnore]
        //[Comment("Роли")]
        //public IEnumerable<string> SetRoles { get => Roles; set => Roles = value.ToList(); }

        /// <summary>
        /// Не для ролей
        /// </summary>
        [Display(Name = nameof(AppRes.InvertRoles), ResourceType = typeof(AppRes))]
        public bool RolesInverse { get; set; }

        [Display(Name = nameof(AppRes.Disable), ResourceType = typeof(AppRes))]
        public bool Disabled { get; set; }

        [Display(Name = nameof(AppRes.Tags), ResourceType = typeof(AppRes))]
        public string[] Tags { get; set; } = [];

        //extra

        public IEnumerable<string> SetRoles { get => Roles; set => Roles = value.ToList(); }


        public static async Task<NavMenu> GetAction(IMarsWebApiClient client, Guid id)
        {
            var item = await client.NavMenu.Get(id) ?? throw new NotFoundException();
            return ToModel(item);
        }

        public static async Task<NavMenu> SaveAction(IMarsWebApiClient client, NavMenu navMenu, bool isNew)
        {
            if (isNew)
            {
                var createdId = await client.NavMenu.Create(navMenu.ToCreateRequest());
                navMenu.Id = createdId;
            }
            else
            {
                await client.NavMenu.Update(navMenu.ToUpdateRequest());
            }
            return navMenu;
        }

        public static Task DeleteAction(IMarsWebApiClient client, NavMenu navMenu)
        {
            return client.NavMenu.Delete(navMenu.Id);
        }

        public CreateNavMenuRequest ToCreateRequest()
            => new()
            {
                Id = null,
                Title = Title,
                Slug = Slug,
                Class = Class,
                Style = Style,
                Disabled = Disabled,
                Roles = Roles,
                RolesInverse = RolesInverse,
                Tags = Tags,
                MenuItems = MenuItems.Select(x => x.ToCreateRequest()).ToList(),
            };

        public UpdateNavMenuRequest ToUpdateRequest()
            => new()
            {
                Id = Id,
                Title = Title,
                Slug = Slug,
                Class = Class,
                Style = Style,
                Disabled = Disabled,
                Roles = Roles,
                RolesInverse = RolesInverse,
                Tags = Tags,
                MenuItems = MenuItems.Select(x => x.ToUpdateRequest()).ToList(),
            };

        public static NavMenu ToModel(NavMenuDetailResponse response)
            => new()
            {
                Id = response.Id,
                Title = response.Title,
                Slug = response.Slug,
                Class = response.Class,
                Style = response.Style,
                Disabled = response.Disabled,
                CreatedAt = response.CreatedAt,
                ModifiedAt = response.ModifiedAt,
                Roles = response.Roles.ToList(),
                RolesInverse = response.RolesInverse,
                Tags = response.Tags.ToArray(),
                MenuItems = response.MenuItems.Select(NavMenuItem.ToModel).ToList(),
            };
    }

}
