using Mars.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mars.Pages.Setup;

public class UserModel : PageModel
{
    private readonly SetupService _setupService;

    [BindProperty]
    public string FirstName { get; set; } = "Admin";

    [BindProperty]
    public string Email { get; set; } = "admin@example.com";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public UserModel(SetupService setupService)
    {
        _setupService = setupService;
    }

    public IActionResult OnGet()
    {
        // If DB config was not set (direct access), go back
        if (string.IsNullOrEmpty(_setupService.DbHost))
        {
            return RedirectToPage("/setup/database");
        }

        // Restore from service if going back
        FirstName = _setupService.AdminFirstName;
        Email = _setupService.AdminEmail;

        return Page();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            ErrorMessage = "Пароль должен содержать не менее 6 символов.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            ErrorMessage = "Введите корректный email.";
            return Page();
        }

        // Save to service
        _setupService.AdminEmail = Email;
        _setupService.AdminPassword = Password;
        _setupService.AdminFirstName = FirstName;

        return RedirectToPage("/setup/complete");
    }
}
