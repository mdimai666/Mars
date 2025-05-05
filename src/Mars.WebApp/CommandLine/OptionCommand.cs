using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mars.Core.Extensions;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Microsoft.Extensions.Localization;

namespace Mars.CommandLine;

public class OptionCommand : CommandCli
{
    public OptionCommand(CommandLineApi cli) : base(cli)
    {
        var optionFilter = new Option<string>(["--filter", "-f"], "Reg ex filter result");

        var optionsCommand = new Command("option", "option manage subcommand");

        var optionListCommand = new Command("list", "list options") { optionFilter };
        optionListCommand.SetHandler(OptionListCommand, optionFilter);
        optionsCommand.AddCommand(optionListCommand);

        var optionKey = new Argument<string>("key", "option key");
        var optionShowCommand = new Command("show", "show option") { optionKey };
        optionShowCommand.SetHandler(OptionShowCommand, optionKey);
        optionsCommand.AddCommand(optionShowCommand);

        var argumentValue = new Argument<bool?>("value", "set bool <true|false>");
        var maintenanceModeCommand = new Command("maintenance", "set MaintenanceModeOption") { argumentValue };
        maintenanceModeCommand.SetHandler(SetMaintenanceModeOptionCommand, argumentValue);
        optionsCommand.AddCommand(maintenanceModeCommand);

        cli.AddCommand(optionsCommand);
    }

    public async Task OptionListCommand(string? filter)
    {
        using var scope = app.Services.CreateScope();
        //var marsDbContextFactory = scope.ServiceProvider.GetRequiredService<IMarsDbContextFactory>();
        //using var ef = MarsDbContextFactory.CreateInstance();
        var optionRepo = scope.ServiceProvider.GetRequiredService<IOptionRepository>();
        var L = scope.ServiceProvider.GetRequiredService<IStringLocalizer>();

        JsonSerializerOptions opt = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        Func<string, string> outFormatJson = (json) => JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonNode>(json), opt);

        var options = await optionRepo.ListAll(default);
        var optionsArray = options.Select(s => new string[] { s.Key, outFormatJson(s.Value).TextEllipsis(500) });

        if (filter is not null)
        {
            Regex re = new Regex(filter);
            optionsArray = optionsArray.Where(s => s.Any(re.IsMatch)).ToList();
        }

        //var table = new ConsoleTable([
        //    ["Option key", "Json value"],
        //    ..optionsArray
        //]);

        //Console.WriteLine(table);

        foreach (var option in optionsArray)
        {
            OutValue(option[0], option[1]);
        }
    }

    public async Task OptionShowCommand(string key)
    {
        if (key is null)
        {
            Console.WriteLine("key must set");
            return;
        }

        using var scope = app.Services.CreateScope();
        //var marsDbContextFactory = scope.ServiceProvider.GetRequiredService<IMarsDbContextFactory>();
        //using var ef = MarsDbContextFactory.CreateInstance();
        var optionRepo = scope.ServiceProvider.GetRequiredService<IOptionRepository>();

        var L = scope.ServiceProvider.GetRequiredService<IStringLocalizer>();

        JsonSerializerOptions opt = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        Func<string, string> outFormatJson = (json) => JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonNode>(json), opt);

        //var option = ef.Options.FirstOrDefault(s => s.Key == key)
        var option = (await optionRepo.GetKeyRaw(key, default))
            ?? throw new Exception($"Key not found: {key}");

        OutValue(option.Key, outFormatJson(option.Value));
    }

    void OutValue(string key, string value)
    {
        Console.WriteLine("─────────────────────────────────────────");
        Console.WriteLine($"Key = {key}");
        Console.WriteLine(value);
    }

    public void SetMaintenanceModeOptionCommand(bool? enable)
    {
        if (enable is null)
        {
            Console.WriteLine("value must set");
            return;
        }

        using var scope = app.Services.CreateScope();
        var optionService = scope.ServiceProvider.GetRequiredService<IOptionService>();
        var maintenanceModeOption = optionService.GetOption<MaintenanceModeOption>();
        maintenanceModeOption.Enable = enable.Value;
        optionService.SaveOption<MaintenanceModeOption>(maintenanceModeOption);
        OutValue(nameof(MaintenanceModeOption), $"Enable={enable}\n--SUCCESS");
    }
}
