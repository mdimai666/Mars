using Mars.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mars.Pages.Setup;

public class CompleteModel : PageModel
{
    private readonly SetupService _setupService;

    public CompleteModel(SetupService setupService)
    {
        _setupService = setupService;
    }

    public IActionResult OnGet()
    {
        // If admin was not set (direct access), go back
        if (string.IsNullOrEmpty(_setupService.AdminEmail))
        {
            return RedirectToPage("/setup/user");
        }

        // Write config file with all collected data
        _setupService.WriteLocalConfig();

        return Page();
    }

    public IActionResult OnGetFinish()
    {
        // Signal wizard host to stop — main application will start after
        SetupWizardHost.SignalComplete();
        return Page();
    }
}
