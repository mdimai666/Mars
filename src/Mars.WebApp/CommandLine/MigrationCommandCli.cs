using System.CommandLine;
using Mars.Host.Shared.CommandLine;
using Mars.Options.Host;
using Mars.UseStartup.MarsParts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mars.CommandLine;

public class MigrationCommandCli : CommandCli
{
    public MigrationCommandCli(CommandLineApi cli) : base(cli)
    {
        //var optionMigrate = new Option<bool>("-migrate", "run migrate script");

        var migrateCommand = new Command("migrate", "run migrate script");
        migrateCommand.SetAction(RunMigrateCommand);
        cli.AddCommand(migrateCommand);
    }

    void RunMigrateCommand(ParseResult _)
    {
        ILogger<Program> _logger = app.Services.GetRequiredService<ILogger<Program>>();
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
        NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder = new(connectionString);
        app.MarsRequireMigrate(_logger, npgsqlConnectionStringBuilder);
        app.Services.UseMarsOptions();
        app.Services.SeedData(app.Configuration, _logger, true);
    }
}
