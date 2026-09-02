using System.CommandLine;
using System.Text.RegularExpressions;
using Mars.CommandLine.Abstractions;
using Mars.Core.Utils;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Plugin.CommandLine;

/// <summary>Управление установленными плагинами поверх <see cref="IPluginService"/>.</summary>
public class PluginCommandCli : CommandCli
{
    public PluginCommandCli(ICommandLineApi cli) : base(cli)
    {
        var argumentPackageId = new Argument<string>("packageId") { Description = "nuget package id of the plugin" };

        var optionFilter = new Option<string>("--filter", "-f") { Description = "Regular expression filter on title/packageId" };
        var optionVersion = new Option<string>("--version", "-v") { Description = "Version to install (default: latest)" };
        var optionYes = new Option<bool>("--yes", "-y") { Description = "Skip confirmation" };

        var pluginCommand = new Command("plugin", "plugins manage subcommand");

        var listCommand = new Command("list", "list installed plugins") { optionFilter };
        listCommand.SetAction((p, ct) => PluginListCommand(p.GetValue(optionFilter), ct));
        pluginCommand.Subcommands.Add(listCommand);

        var installCommand = new Command("install", "install plugin from nuget") { argumentPackageId, optionVersion };
        installCommand.SetAction((p, ct) => PluginInstallCommand(p.GetRequiredValue(argumentPackageId), p.GetValue(optionVersion), ct));
        pluginCommand.Subcommands.Add(installCommand);

        var disableCommand = new Command("disable", "disable plugin") { argumentPackageId };
        disableCommand.SetAction((p, ct) => PluginSetEnabledCommand(p.GetRequiredValue(argumentPackageId), false, ct));
        pluginCommand.Subcommands.Add(disableCommand);

        var enableCommand = new Command("enable", "enable plugin") { argumentPackageId };
        enableCommand.SetAction((p, ct) => PluginSetEnabledCommand(p.GetRequiredValue(argumentPackageId), true, ct));
        pluginCommand.Subcommands.Add(enableCommand);

        var uninstallCommand = new Command("uninstall", "uninstall plugin (folder removed on next restart)") { argumentPackageId, optionYes };
        uninstallCommand.SetAction((p, ct) => PluginUninstallCommand(p.GetRequiredValue(argumentPackageId), p.GetValue(optionYes), ct));
        pluginCommand.Subcommands.Add(uninstallCommand);

        cli.AddCommand(pluginCommand);
    }

    public Task PluginListCommand(string? filter, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();

        var items = pluginService.List(new ListPluginQuery()).Items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            Regex regex;
            try
            {
                regex = new Regex(filter, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Invalid regular expression '{filter}': {ex.Message}");
                Console.ResetColor();
                return Task.CompletedTask;
            }
            items = items.Where(s => regex.IsMatch(s.Title) || regex.IsMatch(s.PackageId));
        }

        var rows = items.Select(s => new string[]
        {
            s.PackageId,
            s.Version,
            s.Enabled ? "yes" : "no",
            s.Source.ToString(),
            s.Locked ? "yes" : "no",
            s.PendingDelete ? "yes" : "no",
        }).ToList();

        var table = new ConsoleTable([
            ["PackageId", "Version", "Enabled", "Source", "Locked", "PendingDelete"],
            .. rows
        ]);

        Console.WriteLine(table);
        return Task.CompletedTask;
    }

    public async Task PluginInstallCommand(string packageId, string? version, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();

        try
        {
            var result = await pluginService.InstallFromNuget(packageId, version, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Plugin '{result.PackageId}' {result.Version} installed successfully.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }

    public async Task PluginSetEnabledCommand(string packageId, bool enabled, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();

        try
        {
            await pluginService.SetEnabled(packageId, enabled);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(enabled
                ? $"Plugin '{packageId}' enabled."
                : $"Plugin '{packageId}' disabled.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }

    public async Task PluginUninstallCommand(string packageId, bool yes, CancellationToken cancellationToken)
    {
        using var scope = app.Services.CreateScope();
        var pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();

        try
        {
            if (yes || Confirm($"Do you really want to uninstall plugin - {packageId}?"))
            {
                await pluginService.Uninstall(packageId);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Plugin '{packageId}' marked for deletion. It will be removed on next restart.");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}
