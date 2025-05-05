using System.ComponentModel.DataAnnotations;
using Aqua.EnumerableExtensions;
using Mars.Shared.Contracts.Users;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.UserViews;

public class CreateUserModel
{
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.FirstName), ResourceType = typeof(AppRes))]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.LastName), ResourceType = typeof(AppRes))]
    public string LastName { get; set; } = "";

    [Display(Name = nameof(AppRes.MiddleName), ResourceType = typeof(AppRes))]
    public string MiddleName { get; set; } = "";

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [EmailAddress(ErrorMessageResourceName = nameof(AppRes.v_email), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.Email), ResourceType = typeof(AppRes))]
    public string Email { get; set; } = "";

    [StringLength(30, MinimumLength = 6, ErrorMessageResourceName = nameof(AppRes.v_range), ErrorMessageResourceType = typeof(AppRes))]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.Password), ResourceType = typeof(AppRes))]
    public string Password { get; set; } = "";

    [Display(Name = nameof(AppRes.Roles), ResourceType = typeof(AppRes))]
    public IReadOnlyCollection<string> Roles { get; set; } = [];

    public CreateUserRequest ToCreateRequest()
        => new()
        {
            FirstName = FirstName,
            LastName = LastName.AsNullIfEmpty(),
            MiddleName = MiddleName.AsNullIfEmpty(),
            Email = Email.AsNullIfEmpty(),
            Password = Password,
            Roles = Roles,

            PhoneNumber = null,
            BirthDate = null,
            Gender = UserGender.None,
        };

    public static async Task<CreateUserModel> SaveAction(IMarsWebApiClient client, CreateUserModel data, bool isNew)
    {
        if (isNew)
        {
            //var createdId = await client.User.Create(data.ToCreateRequest());
            await client.User.Create(data.ToCreateRequest());
            //user.Id = createdId;
        }
        else
        {
            //await client.NavMenu.Update(navMenu.ToUpdateRequest());
            throw new NotImplementedException();
        }
        return data;
    }

    //public static Task DeleteAction(IMarsWebApiClient client, CreateUserModel user)
    //{
    //    return client.NavMenu.Delete(user.Id);
    //}

    //public static CreateUserModel ToModel(CreateUserRequest response)
    //        => new()
    //        {
    //            Id = response.Id,
    //            Title = response.Title,
    //            Slug = response.Slug,
    //            Class = response.Class,
    //            Style = response.Style,
    //            Disabled = response.Disabled,
    //            CreatedAt = response.CreatedAt,
    //            ModifiedAt = response.ModifiedAt,
    //            Roles = response.Roles.ToList(),
    //            RolesInverse = response.RolesInverse,
    //            Tags = response.Tags.ToList(),
    //            MenuItems = response.MenuItems.Select(NavMenuItem.ToModel).ToList(),
    //        };
}
