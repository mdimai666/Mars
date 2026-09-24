using System.CommandLine;
using Mars.CommandLine.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Mars.CommandLine.Scripting;

/// <summary>
/// `Mars.exe fn` — одноразовое исполнение C#-исходника (top-level statements) в живом инстансе:
/// настройка среды, девопс, автоматизация (полный DI через globals <see cref="FnContext"/>).
/// Exit codes: 0 — успех, 1 — ошибка запуска/исполнения, 2 — ошибка компиляции скрипта.
/// ВНИМАНИЕ: произвольный код с правами процесса — доверие как к локальному UDS-сокету (migrate/node inject).
/// </summary>
public class FnCommandCli : CommandCli
{
    public const string CommandName = "fn";

    public FnCommandCli(ICommandLineApi cli) : base(cli)
    {
        var fileOption = new Option<string?>("--file", "-f")
        {
            Description = "C# file to run; '-' reads the code from stdin (stdin is not available for remote execution — use --local)",
        };
        var codeOption = new Option<string?>("--code", "-c")
        {
            Description = "inline C# code to run",
        };
        var scriptArgsArgument = new Argument<string[]>("args")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "arguments passed to the script (FnContext.Args)",
        };

        var fnCommand = new Command(CommandName,
            "run C# code (top-level statements) once inside the running Mars instance; " +
            "globals: GetService<T>(), GetRequiredService<T>(), Services, CreateScope(), Args, CancellationToken")
        {
            fileOption,
            codeOption,
            scriptArgsArgument,
        };

        fnCommand.SetAction(async (parseResult, ct) =>
        {
            var file = parseResult.GetValue(fileOption);
            var code = parseResult.GetValue(codeOption);
            var args = parseResult.GetValue(scriptArgsArgument) ?? [];
            return await ExecuteAsync(file, code, args, cli.InRemoteInvocation, app.Services, ct);
        });

        cli.AddCommand(fnCommand);
    }

    internal static async Task<int> ExecuteAsync(
        string? file,
        string? code,
        string[] args,
        bool inRemoteInvocation,
        IServiceProvider services,
        CancellationToken ct)
    {
        var (source, error) = await ReadSourceAsync(file, code, inRemoteInvocation, ct);
        if (error != null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        var context = new FnContext(services, args, ct);

        try
        {
            var state = await FnScriptRunner.RunAsync(source!, context, ct);

            if (state.Exception != null)
            {
                Console.Error.WriteLine(state.Exception.ToString());
                return 1;
            }

            if (state.ReturnValue != null)
                Console.WriteLine(state.ReturnValue);

            return 0;
        }
        catch (CompilationErrorException e)
        {
            foreach (var diagnostic in e.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                Console.Error.WriteLine(diagnostic.ToString());
            return 2;
        }
    }

    internal static async Task<(string? Code, string? Error)> ReadSourceAsync(
        string? file, string? code, bool inRemoteInvocation, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(code))
        {
            if (!string.IsNullOrEmpty(file))
                return (null, "fn: --file and --code are mutually exclusive");
            return (code, null);
        }

        if (string.IsNullOrEmpty(file))
            return (null, "fn: specify the source: --file <path> ('-' = stdin) or --code \"...\"");

        if (file == "-")
        {
            // в remote-исполнении Console.In — stdin СЕРВЕРА, не клиента: код туда не доедет
            if (inRemoteInvocation)
                return (null,
                    "fn: stdin source is not available for remote execution (the command runs inside the server process); " +
                    "pass a file path or use --local");

            var piped = await Console.In.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(piped))
                return (null, "fn: no code received from stdin");
            return (piped, null);
        }

        if (!File.Exists(file))
            return (null, $"fn: file not found: {file}");

        return (await File.ReadAllTextAsync(file, ct), null);
    }
}
