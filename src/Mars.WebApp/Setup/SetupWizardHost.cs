namespace Mars.Setup;

public static class SetupWizardHost
{
    private static readonly TaskCompletionSource _completionSource = new();

    public static Task Completion => _completionSource.Task;

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
