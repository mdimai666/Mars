using System.ComponentModel.DataAnnotations;
using Mars.Core.Extensions;
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

    [Display(Name = nameof(AppRes.UserType), ResourceType = typeof(AppRes))]
    public string Type { get; set; } = "";

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
            Type = Type,

            MetaValues = []
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

}
