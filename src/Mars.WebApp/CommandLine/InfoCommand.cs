using System.CommandLine;
using Mars.Host.Data.Constants;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Services;
using Mars.UseStartup;
using Npgsql;

namespace Mars.CommandLine;

public class InfoCommand : CommandCli
{
    public InfoCommand(CommandLineApi cli) : base(cli)
    {
        var infoCommand = new Command("info", "show main info");
        infoCommand.SetAction((_) => ShowInfoCommand());
        cli.AddCommand(infoCommand);

    }

    public void ShowInfoCommand(bool showHello = true)
    {
        if (showHello)
            Console.WriteLine(Mars.Core.Extensions.MarsStringExtensions.HelloText());

        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
        string databaseName;

        if (connectionString.StartsWith(DatabaseProviderConstants.InMemoryDb))
        {
            databaseName = DatabaseProviderConstants.InMemoryDb;
        }
        else
        {
            var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            databaseName = npgsqlConnectionStringBuilder.Database!;
        }

        var sp = app.Services;
        var env = sp.GetRequiredService<IHostEnvironment>();
        var wwwRoot = Path.Join(env.ContentRootPath, "wwwroot");
        _ = nameof(IOptionService.FileHostingInfo);// see for sync
        var uploadPath = Path.Join(wwwRoot, "upload");

        Console.WriteLine("version = " + MarsStartupInfo.Version);
        Console.WriteLine("wwwroot = " + wwwRoot);
        Console.WriteLine("upload = " + uploadPath);
        Console.WriteLine("Database = " + databaseName);
        Console.WriteLine("EnvMode = " + env.EnvironmentName);
        if (StartupFront.AppProvider.SetupMultiApps)
        {
            Console.WriteLine("App fronts:");
            foreach (var af in StartupFront.AppProvider.Apps.Values)
            {
                Console.WriteLine($"[\"{af.Configuration.Url}\", {af.Configuration.Mode}] {af.Configuration.Path}");
            }
        }
        else
        {
            Console.WriteLine("Mode = " + StartupFront.AppProvider.FirstApp.Configuration.Mode);
        }
    }
}
