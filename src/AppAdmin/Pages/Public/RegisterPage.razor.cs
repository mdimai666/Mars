using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using AppFront.Shared.AuthProviders;
using Mars.Core.Extensions;
using Mars.Shared.Contracts.Auth;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;

namespace AppAdmin.Pages.Public;

public partial class RegisterPage
{
    class UserForRegistrationModel
    {
        [EmailAddress(ErrorMessageResourceName = nameof(AppRes.EmailInvalidMessage), ErrorMessageResourceType = typeof(AppRes))]
        [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = "";

        [Display(Name = "Пароль")]
        [Category("Security")]
        [Description("Account password")]
        [PasswordPropertyText(true)]
        [Required(ErrorMessageResourceName = nameof(AppRes.v_required), ErrorMessageResourceType = typeof(AppRes))]
        public string Password { get; set; } = "";

        [Display(Name = "Повторите пароль")]
        [Category("Security")]
        [PasswordPropertyText(true)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Display(Name = nameof(AppRes.FirstName), ResourceType = typeof(AppRes))]
        public string FirstName { get; set; } = "";

        [Display(Name = nameof(AppRes.LastName), ResourceType = typeof(AppRes))]
        public string LastName { get; set; } = "";

        public UserForRegistrationRequest ToRequest()
            => new()
            {
                Email = Email,
                Password = Password,
                FirstName = FirstName.AsNullIfEmpty(),
                LastName = LastName.AsNullIfEmpty(),
            };
    }

    [CascadingParameter]
    public Task<AuthenticationState> AuthState { get; set; } = default!;

    UserForRegistrationModel _userForRegistration = new();
    bool _acceptPrivacy = false;

    [Inject] public IAuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    public bool ShowRegistrationErrors { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    private bool IsAlreadyAuth = false;

    bool regiterComplete = false;


    protected override async Task<Task> OnInitializedAsync()
    {

        var authState = await AuthState;

        IsAlreadyAuth = authState?.User?.Identity?.IsAuthenticated ?? false;

        if (IsAlreadyAuth)
        {
            NavigationManager.NavigateTo("/");
        }

        return base.OnInitializedAsync();
    }

    public async Task ExecuteRegister(EditContext editContext)
    {
        if (!editContext.Validate()) return;

        ShowRegistrationErrors = false;
        var result = await AuthenticationService.RegisterUser(_userForRegistration.ToRequest());
        if (!result.IsSuccessfulRegistration)
        {
            Errors = result.Errors;
            ShowRegistrationErrors = true;
        }
        else
        {
            //NavigationManager.NavigateTo("/");
            regiterComplete = true;
            StateHasChanged();
        }
    }
}
