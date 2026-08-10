using Microsoft.Extensions.Configuration;
using static Mars.UseStartup.MarsStartupInfo;

namespace Mars.Setup;

public static class SetupWizardHost
{
    private static readonly TaskCompletionSource _completionSource = new();

    public static Task Completion => _completionSource.Task;

    /// <summary>
    /// Куда визард пишет итоговый конфиг (относительно рабочей директории):
    /// в Docker — на примонтированный том ./config, иначе — appsettings.Local.json рядом с приложением.
    /// </summary>
    public static string WizardConfigPath => IsRunningInDocker
        ? Path.Combine("config", "appsettings.Production.json")
        : "appsettings.Local.json";

    /// <summary>
    /// Визард запускается, когда приложение не сконфигурировано.
    /// В Docker учитываются только явные источники: env-переменные, примонтированный
    /// appsettings.Production.json и конфиг визарда на томе ./config — девелоперские
    /// дефолты из appsettings.json внутри образа конфигурацией не считаются.
    /// Отключается через MARS_SETUP_WIZARD=0.
    /// </summary>
    public static bool ShouldRunWizard()
    {
        if (IsTesting) return false;

        var killSwitch = Environment.GetEnvironmentVariable("MARS_SETUP_WIZARD");
        if (killSwitch is "0" || killSwitch is not null && killSwitch.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        if (IsRunningInDocker)
        {
            var probe = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine("config", "appsettings.Production.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            return string.IsNullOrWhiteSpace(probe.GetConnectionString("DefaultConnection"));
        }

        return !File.Exists("appsettings.Local.json");
    }

    public static void SignalComplete() => _completionSource.TrySetResult();

    public static async Task RunAsync(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║          Mars Setup Wizard              ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.WriteLine();

        var contentRoot = Path.Combine(Directory.GetCurrentDirectory());
        var wwwRoot = Path.Combine(contentRoot, "wwwroot");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = contentRoot,
            WebRootPath = wwwRoot,
        });
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton<SetupService>();

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (!path.StartsWithSegments("/setup")
                && !path.StartsWithSegments("/mars")
                && !path.StartsWithSegments("/css")
                && !path.StartsWithSegments("/js")
                && !path.StartsWithSegments("/img")
                && !path.StartsWithSegments("/favicon.ico"))
            {
                context.Response.Redirect("/setup");
                return;
            }
            await next();
        });
        app.MapRazorPages();

        await app.StartAsync();

        Console.WriteLine($"  Wizard started. Open: {app.Urls.FirstOrDefault() ?? "http://localhost"}/setup");
        Console.WriteLine();

        // Wait until setup is complete (signaled from Complete page)
        await _completionSource.Task;

        await app.StopAsync();
        await app.DisposeAsync();

        Console.WriteLine("  Wizard completed. Starting main application...");
        Console.WriteLine();
    }
}
