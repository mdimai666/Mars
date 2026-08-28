using System.CommandLine;
using Mars.CommandLine.Abstractions;
using Mars.Data.Constants;
using Mars.Server.Startup;
using Mars.SiteEngine.Abstractions.Services;
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
        try
        {
            var fronts = sp.GetRequiredService<IFrontManager>().Fronts.Where(s => s.Enabled).ToList();
            if (fronts.Count > 1)
            {
                Console.WriteLine("App fronts:");
                foreach (var front in fronts)
                {
                    Console.WriteLine($"[\"{(string.IsNullOrEmpty(front.Url) ? "/" : front.Url)}\", {front.EngineId}] {front.Slug}");
                }
            }
            else if (fronts.Count == 1)
            {
                var front = fronts[0];
                Console.WriteLine($"Front = '{front.Slug}' ({front.EngineId})");
            }
            else
            {
                Console.WriteLine("App fronts: нет включённых");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("App fronts: недоступны (" + ex.Message + ")");
        }
    }
}
