using Mars.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mars.Pages.Setup;

public class CompleteModel : PageModel
{
    public IActionResult OnGet()
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
        if (!System.IO.File.Exists(configPath))
        {
            return RedirectToPage("/setup/database");
        }
        return Page();
    }

    public IActionResult OnGetFinish()
    {
        // Signal wizard host to stop — main application will start after
        SetupWizardHost.SignalComplete();
        return Page();
    }
}
