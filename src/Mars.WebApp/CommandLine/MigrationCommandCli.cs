using System.CommandLine;
using Mars.Host.Shared.CommandLine;
using Mars.UseStartup.MarsParts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mars.CommandLine;

public class MigrationCommandCli : CommandCli
{
    public MigrationCommandCli(CommandLineApi cli) : base(cli)
    {
        //var optionMigrate = new Option<bool>("-migrate", "run migrate script");

        var migrateCommand = new Command("-migrate", "run migrate script");
        migrateCommand.SetHandler(RunMigrateCommand);
        cli.AddCommand(migrateCommand);
    }

    void RunMigrateCommand()
    {
        ILogger<Program> _logger = app.Services.GetRequiredService<ILogger<Program>>();
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
        NpgsqlConnectionStringBuilder npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        app.MarsRequireMigrate(_logger, npgsqlConnectionStringBuilder);
    }
}
