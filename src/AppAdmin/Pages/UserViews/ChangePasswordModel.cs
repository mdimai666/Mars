using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Users;
using Mars.Shared.Resources;
using Mars.WebApiClient.Interfaces;
using Newtonsoft.Json;

namespace AppAdmin.Pages.UserViews;

public class ChangePasswordModel
{
    public Guid UserId { get; set; }

    [Display(Name = nameof(AppRes.NewPassword), ResourceType = typeof(AppRes))]
    [Range(6, 30, ErrorMessageResourceName = nameof(AppRes.v_range), ErrorMessageResourceType = typeof(AppRes))]
    [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
    public string NewPassword { get; set; } = "";

    public SetUserPasswordByIdRequest ToRequest()
        => new()
        {
            UserId = UserId,
            NewPassword = NewPassword,
        };

    public static async Task<ChangePasswordModel> SaveAction(IMarsWebApiClient client, ChangePasswordModel data)
    {
        if (data.UserId == Guid.Empty) throw new ArgumentException("UserId cannot be empty");
        var result = await client.User.SetPassword(data.ToRequest());
        return data;
    }

}
