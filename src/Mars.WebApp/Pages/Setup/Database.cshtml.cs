using Mars.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mars.Pages.Setup;

public class DatabaseModel : PageModel
{
    private readonly SetupService _setupService;

    [BindProperty]
    public string Host { get; set; } = "127.0.0.1";

    [BindProperty]
    public int Port { get; set; } = 5432;

    [BindProperty]
    public string Database { get; set; } = "mars";

    [BindProperty]
    public string Username { get; set; } = "mars";

    [BindProperty]
    public string Password { get; set; } = "mars";

    public string? TestResult { get; set; }
    public bool TestSuccess { get; set; }

    public DatabaseModel(SetupService setupService)
    {
        _setupService = setupService;
    }

    public void OnGet()
    {
        TestResult = null;
    }

    public async Task<IActionResult> OnPostTestDbAsync()
    {
        var (success, message) = await _setupService.TestDatabaseConnectionAsync(
            Host, Port, Database, Username, Password);

        TestResult = message;
        TestSuccess = success;
        return Page();
    }

    public async Task<IActionResult> OnPostNextAsync()
    {
        // Auto-validate connection before proceeding
        var (success, message) = await _setupService.TestDatabaseConnectionAsync(
            Host, Port, Database, Username, Password);

        if (!success)
        {
            TestResult = message;
            TestSuccess = false;
            return Page();
        }

        // Store DB config in TempData for the next step
        TempData["DbHost"] = Host;
        TempData["DbPort"] = Port.ToString();
        TempData["DbName"] = Database;
        TempData["DbUser"] = Username;
        TempData["DbPass"] = Password;

        return RedirectToPage("/setup/user");
    }
}
