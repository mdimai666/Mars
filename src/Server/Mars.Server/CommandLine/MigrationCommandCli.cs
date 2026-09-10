using System.CommandLine;
using Mars.CommandLine.Abstractions;
using Mars.Server.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Mars.Server.CommandLine;

public class MigrationCommandCli : CommandCli
{
    public MigrationCommandCli(ICommandLineApi cli) : base(cli)
    {
        //var optionMigrate = new Option<bool>("-migrate", "run migrate script");

        var migrateCommand = new Command("migrate", "run migrate script");
        migrateCommand.SetAction(RunMigrateCommand);
        cli.AddCommand(migrateCommand);
    }

    void RunMigrateCommand(ParseResult _)
    {
        ILogger<MigrationCommandCli> _logger = app.Services.GetRequiredService<ILogger<MigrationCommandCli>>();
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
        NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder = new(connectionString);
        app.MarsRequireMigrate(_logger, npgsqlConnectionStringBuilder);
        app.Services.UseMarsServerOptions();
        app.Services.SeedData(app.Configuration, _logger, true);
    }
}
