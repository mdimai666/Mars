using System.CommandLine;
using System.Text.Json;
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

    /// <summary>Маркер машиночитаемого ответа (--json): следующая за ним строка — JSON-конверт.</summary>
    public const string ResultMarker = "#MARS-FN-RESULT#";

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
        var jsonOption = new Option<bool>("--json")
        {
            Description = $"machine-readable answer: '{ResultMarker}' line followed by a single-line JSON envelope {{ok, exitCode, result, error}} (replaces the raw result print)",
        };
        var examplesOption = new Option<bool>("--examples")
        {
            Description = "show usage examples and exit",
        };
        var scriptArgsArgument = new Argument<string[]>("args")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "arguments passed to the script (FnContext.Args)",
        };

        var fnCommand = new Command(CommandName, "run a C# script once inside the running Mars instance")
        {
            fileOption,
            codeOption,
            jsonOption,
            examplesOption,
            scriptArgsArgument,
        };

        fnCommand.SetAction(async (parseResult, ct) =>
        {
            if (parseResult.GetValue(examplesOption))
            {
                Console.WriteLine(UsageExamples);
                return 0;
            }

            var file = parseResult.GetValue(fileOption);
            var code = parseResult.GetValue(codeOption);
            var json = parseResult.GetValue(jsonOption);
            var args = parseResult.GetValue(scriptArgsArgument) ?? [];
            return await ExecuteAsync(file, code, args, cli.InRemoteInvocation, json, app.Services, ct);
        });

        cli.AddCommand(fnCommand);
    }

    internal const string UsageExamples = """
        Run inline code (result is printed, exit code 0/1/2 = ok/runtime error/compile error):
          Mars.exe fn -c "return 2 + 2;"

        Run a C# file (top-level statements):
          Mars.exe fn -f setup.cs

        Pass arguments to the script (available as Args) and get a machine-readable answer:
          Mars.exe -q fn --json -f setup.cs -- prod
          stdout:
            #MARS-FN-RESULT#
            {"ok":true,"exitCode":0,"result":...,"error":null}

        Pipe code via stdin (in-process only — stdin is not forwarded to a running server):
          Get-Content setup.cs | Mars.exe fn -f - --local

        Inside the script (FnContext globals, no using needed for BCL basics):
          var users = GetService<IUserService>();          // null when not registered
          var posts = GetRequiredService<IPostService>();  // throws when not registered
          using (var scope = CreateScope())                // scope for scoped services
          {
              var db = scope.ServiceProvider.GetRequiredService<MarsDbContext>();
          }
          Console.WriteLine($"args: {Args.Length}");       // script output goes to stdout
          return "done";                                   // return value = result
        """;

    internal static async Task<int> ExecuteAsync(
        string? file,
        string? code,
        string[] args,
        bool inRemoteInvocation,
        bool json,
        IServiceProvider services,
        CancellationToken ct)
    {
        var (source, error) = await ReadSourceAsync(file, code, inRemoteInvocation, ct);
        if (error != null)
        {
            Console.Error.WriteLine(error);
            return Finish(1, json, error: error);
        }

        var context = new FnContext(services, args, ct);

        try
        {
            var state = await FnScriptRunner.RunAsync(source!, context, ct);

            if (state.Exception != null)
            {
                Console.Error.WriteLine(state.Exception.ToString());
                return Finish(1, json, error: state.Exception.ToString());
            }

            if (state.ReturnValue != null && !json)
                Console.WriteLine(state.ReturnValue);

            return Finish(0, json, result: state.ReturnValue);
        }
        catch (CompilationErrorException e)
        {
            var diagnostics = e.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();

            foreach (var diagnostic in diagnostics)
                Console.Error.WriteLine(diagnostic);

            return Finish(2, json, error: string.Join(Environment.NewLine, diagnostics));
        }

        static int Finish(int exitCode, bool json, object? result = null, string? error = null)
        {
            if (!json)
                return exitCode;

            Console.WriteLine(ResultMarker);
            Console.WriteLine(BuildResultJson(exitCode, result, error));
            return exitCode;
        }
    }

    internal static string BuildResultJson(int exitCode, object? result, string? error)
    {
        JsonElement? resultElement = null;
        if (result != null)
        {
            try
            {
                resultElement = JsonSerializer.SerializeToElement(result);
            }
            catch (Exception)
            {
                resultElement = JsonSerializer.SerializeToElement(result.ToString());
            }
        }

        return JsonSerializer.Serialize(
            new FnResultEnvelope(exitCode == 0, exitCode, resultElement, error),
            EnvelopeJsonOptions);
    }

    private static readonly JsonSerializerOptions EnvelopeJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed record FnResultEnvelope(bool Ok, int ExitCode, JsonElement? Result, string? Error);

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
