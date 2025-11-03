using System.ComponentModel.DataAnnotations;
using AppAdmin.Pages.UserTypeViews;
using AppFront.Shared.Components.MetaFieldViews;
using Mars.Core.Attributes;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Shared.Contracts.Roles;
using Mars.Shared.Contracts.Users;
using Mars.Shared.Contracts.UserTypes;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;

namespace AppAdmin.Pages.UserViews;

public class UserEditModel
{
    public Guid Id { get; set; }

    [MinLength(3)]
    [SlugString(true)]
    public string UserName { get; set; } = "";

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
    public string Type { get; set; } = "";

    public List<MetaValueEditModel> MetaValues { get; set; } = [];
    public UserTypeEditModel UserType { get; init; } = new();

    public IReadOnlyCollection<RoleSummaryResponse> AvailRoles { get; init; } = [];

    public static async Task<UserEditModel> GetAction(IMarsWebApiClient client, Guid id, string userTypeName)
    {
        if (id == Guid.Empty)
        {
            ArgumentException.ThrowIfNullOrEmpty(userTypeName, nameof(userTypeName));
            var vm = await client.User.GetUserBlank(userTypeName);
            return FromViewModel(vm);
        }
        else
        {
            var vm = await client.User.GetEditModel(id) ?? throw new NotFoundException();
            return FromViewModel(vm);
        }

    }

    public static async Task<UserEditModel> SaveAction(IMarsWebApiClient client, UserEditModel user, bool isNew)
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
            //return ToModel(updated, user.AvailRoles, roles);
        }
        return user;
    }

    public static Task DeleteAction(IMarsWebApiClient client, UserEditModel model)
    {
        return client.User.Delete(model.Id);
    }

    public CreateUserRequest ToCreateRequest(string password)
        => new()
        {
            UserName = UserName,
            FirstName = FirstName,
            LastName = LastName,
            MiddleName = MiddleName.AsNullIfEmpty(),
            Email = Email.AsNullIfEmpty(),
            Roles = Roles.Select(s => s.Name).ToList(),

            BirthDate = ConvertValid(BirthDate),
            Gender = Gender,
            PhoneNumber = PhoneNumber.AsNullIfEmpty(),

            Password = password,
            Type = Type,
            MetaValues = MetaValues.Select(s => s.ToCreateRequest()).ToList(),
        };

    public UpdateUserRequest ToUpdateRequest()
        => new()
        {
            Id = Id,
            UserName = UserName,
            FirstName = FirstName,
            LastName = LastName,
            MiddleName = MiddleName.AsNullIfEmpty(),
            Email = Email.AsNullIfEmpty(),
            Roles = Roles.Select(s => s.Name).ToList(),

            BirthDate = ConvertValid(BirthDate),
            Gender = Gender,
            PhoneNumber = PhoneNumber.AsNullIfEmpty(),
            Type = Type,
            MetaValues = MetaValues.Select(s => s.ToUpdateRequest()).ToList(),
        };

    public static UserEditModel FromViewModel(UserEditViewModel vm)
        => ToModel(vm.User, vm.AvailRoles, vm.UserType);

    public static UserEditModel ToModel(UserEditResponse response, IEnumerable<RoleSummaryResponse> roles, UserTypeDetailResponse userType)
            => new()
            {
                Id = response.Id,
                UserName = response.UserName,
                FirstName = response.FirstName,
                LastName = response.LastName,
                MiddleName = response.MiddleName ?? "",
                Email = response.Email ?? "",
                Roles = roles.Where(s => response.Roles.Contains(s.Name)).ToList(),

                BirthDate = response.BirthDate,
                Gender = response.Gender,
                PhoneNumber = response.Phone ?? "",
                Type = response.Type ?? "",
                MetaValues = response.MetaValues.Select(MetaValueEditModel.ToModel).ToList(),

                //extra
                UserType = UserTypeEditModel.ToModel(userType, []),
                AvailRoles = roles.ToList(),
            };

    public static DateTime? ConvertValid(DateTime? dateTime)
    {
        if (dateTime is null) return null;
        if (dateTime == DateTime.MinValue) return null;
        return dateTime;
    }
}
