using System.CommandLine;
using System.Text.RegularExpressions;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Utils;
using Microsoft.Extensions.Localization;

namespace Mars.CommandLine;

public class RoleCommandCli : CommandCli
{
    public RoleCommandCli(CommandLineApi cli) : base(cli)
    {
        var optionFilter = new Option<string>(["--filter", "-f"], "Reg ex filter result");

        var roleCommand = new Command("role", "role manage subcommand");

        var roleListCommand = new Command("list", "list roles") { optionFilter };
        roleListCommand.SetHandler(RoleListCommand, optionFilter);
        roleCommand.AddCommand(roleListCommand);
        cli.AddCommand(roleCommand);
    }

    public async Task RoleListCommand(string? filter)
    {
        using var scope = app.Services.CreateScope();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var L = scope.ServiceProvider.GetRequiredService<IStringLocalizer>();

        //var roles = ef.Roles.ToList();
        var roles = await roleRepo.ListAll(default);
        var rolesArray = roles.Select(s => new string[] { s.Name, L[s.Name] });

        if (filter is not null)
        {
            Regex re = new Regex(filter);
            rolesArray = rolesArray.Where(s => s.Any(re.IsMatch)).ToList();
        }

        var table = new ConsoleTable([
            ["Role Name", "Display name"],
            ..rolesArray
        ]);

        Console.WriteLine(table);
    }
}
