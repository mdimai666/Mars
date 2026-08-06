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
        // If DB config was lost (direct access), go back
        if (string.IsNullOrEmpty(TempData.Peek("DbHost")?.ToString()))
        {
            return RedirectToPage("/setup/database");
        }

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

        var dbHost = TempData["DbHost"]?.ToString()!;
        var dbPort = int.Parse(TempData["DbPort"]?.ToString()!);
        var dbName = TempData["DbName"]?.ToString()!;
        var dbUser = TempData["DbUser"]?.ToString()!;
        var dbPass = TempData["DbPass"]?.ToString()!;

        _setupService.WriteLocalConfig(
            dbHost, dbPort, dbName, dbUser, dbPass,
            Email, Password, FirstName);

        // Redirect to complete page, then stop the wizard host
        return RedirectToPage("/setup/complete");
    }
}
