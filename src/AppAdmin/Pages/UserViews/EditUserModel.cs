using System.ComponentModel.DataAnnotations;
using Aqua.EnumerableExtensions;
using Mars.Core.Attributes;
using Mars.Core.Extensions;
using Mars.Shared.Contracts.Roles;
using Mars.Shared.Contracts.Users;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.UserViews;

public class EditUserModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.FirstName), ResourceType = typeof(AppRes))]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.LastName), ResourceType = typeof(AppRes))]
    public string LastName { get; set; } = "";

    [Display(Name = nameof(AppRes.MiddleName), ResourceType = typeof(AppRes))]
    public string MiddleName { get; set; } = "";

    public string FullName => string.Join(' ', ((string?[])[LastName, FirstName, MiddleName]).TrimNulls());


    [EmailAddressThatAllowsBlanks(ErrorMessageResourceName = nameof(AppRes.v_email), ErrorMessageResourceType = typeof(AppRes))]
    [Display(Name = nameof(AppRes.Email), ResourceType = typeof(AppRes))]
    public string Email { get; set; } = "";

    [Display(Name = nameof(AppRes.Roles), ResourceType = typeof(AppRes))]
    public IEnumerable<RoleSummaryResponse> Roles { get; set; } = [];


    [Display(Name = nameof(AppRes.Phone), ResourceType = typeof(AppRes))]
    [RegularExpression(@"^\+?\d{3,12}$", ErrorMessageResourceName = nameof(AppRes.InvalidPhoneNumberError), ErrorMessageResourceType = typeof(AppRes))]
    public string PhoneNumber { get; set; } = "";

    [Display(Name = nameof(AppRes.BirthDate), ResourceType = typeof(AppRes))]
    public DateTime? BirthDate { get; set; }

    [Display(Name = nameof(AppRes.Gender), ResourceType = typeof(AppRes))]
    public UserGender Gender { get; set; }


    public static async Task<EditUserModel> SaveAction(IMarsWebApiClient client, EditUserModel user, bool isNew, IEnumerable<RoleSummaryResponse> roles)
    {
        if (isNew)
        {
            //var created = await client.User.Create(user.ToCreateRequest());
            //return ToModel(created, roles);
            throw new NotImplementedException();
        }
        else
        {
            var updated = await client.User.Update(user.ToUpdateRequest());
            return ToModel(updated, roles);
        }
    }

    public static Task DeleteAction(IMarsWebApiClient client, EditUserModel model)
    {
        return client.User.Delete(model.Id);
    }

    public CreateUserRequest ToCreateRequest(string password)
        => new()
        {
            FirstName = FirstName,
            LastName = LastName,
            MiddleName = MiddleName.AsNullIfEmpty(),
            Email = Email.AsNullIfEmpty(),
            Roles = Roles.Select(s => s.Name).ToList(),

            BirthDate = ConvertValid(BirthDate),
            Gender = Gender,
            PhoneNumber = PhoneNumber.AsNullIfEmpty(),

            Password = password,
        };

    public UpdateUserRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            MiddleName = MiddleName.AsNullIfEmpty(),
            Email = Email.AsNullIfEmpty(),
            Roles = Roles.Select(s => s.Name).ToList(),

            BirthDate = ConvertValid(BirthDate),
            Gender = Gender,
            PhoneNumber = PhoneNumber.AsNullIfEmpty(),
        };

    public static EditUserModel ToModel(UserDetailResponse response, IEnumerable<RoleSummaryResponse> roles)
            => new()
            {
                Id = response.Id,
                FirstName = response.FirstName,
                LastName = response.LastName,
                MiddleName = response.MiddleName ?? "",
                Email = response.Email ?? "",
                Roles = roles.Where(s => response.Roles.Contains(s.Name)).ToList(),

                BirthDate = response.BirthDate,
                Gender = response.Gender,
                PhoneNumber = response.PhoneNumber ?? "",
            };

    public static DateTime? ConvertValid(DateTime? dateTime)
    {
        if (dateTime is null) return null;
        if (dateTime == DateTime.MinValue) return null;
        return dateTime;
    }
}
