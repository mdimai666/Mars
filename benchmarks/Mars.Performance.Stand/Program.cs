using System.Reflection;
using Mars;
using Mars.E2E.Tests.Fixtures;
using Mars.Performance.Stand;
using Mars.Test.Common.Constants;

// DatabaseFixture читает appsettings.json из CWD — фиксируем его на каталоге стенда,
// чтобы запуск из корня репозитория не ломал резолв конфигов.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var options = StandOptions.Parse(args);

if (options.AttachUrl is not null)
{
    return await RunAttachModeAsync(options.AttachUrl);
}

// Версия приложения — из сборки Mars.WebApp (<Version>$(MarsAppVersion)</Version>)
var appVersion = typeof(MarsWebAppStartup).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

Console.WriteLine("=== Mars Performance Stand ===");
Console.WriteLine($"Версия приложения: {appVersion}");
Console.WriteLine($"Поднимаю Kestrel + Testcontainers PostgreSQL; постов для чтения: {options.PostsCount}");
Console.WriteLine();

var fixture = new E2EServerFixture();
await fixture.InitializeAsync();

var exitCode = 0;
try
{
    await fixture.Seed();
    PerfSeeder.EnsureFront(fixture.ServiceProvider);
    await PerfSeeder.SeedPostsAsync(fixture.ServiceProvider, options.PostsCount);

    await fixture.WarmupRenderer();
    await PerfSeeder.WarmupHttpAsync(fixture.BaseUrl);

    var failures = await PerfSeeder.SmokeAsync(fixture.BaseUrl);
    if (failures > 0) exitCode = 1;

    PrintStandInfo(fixture.BaseUrl, appVersion, options.PostsCount);
    PerfSeeder.PrintK6Hint(fixture.BaseUrl);

    // ready-файл: строка 1 — URL, строка 2 — версия приложения (для run-perf.ps1)
    if (options.ReadyFile is not null)
        await File.WriteAllTextAsync(options.ReadyFile, $"{fixture.BaseUrl}\n{appVersion}");

    if (!options.NoWait)
    {
        await WaitUntilCancelled(options.WaitMinutes > 0 ? TimeSpan.FromMinutes(options.WaitMinutes) : null);
    }
}
finally
{
    if (options.ReadyFile is not null && File.Exists(options.ReadyFile))
        File.Delete(options.ReadyFile);
    await fixture.DisposeAsync();
}

return exitCode;

static async Task<int> RunAttachModeAsync(string attachUrl)
{
    Console.WriteLine($"Attach mode: приложение должно быть запущено на {attachUrl}");
    using var http = new HttpClient { BaseAddress = new Uri(attachUrl), Timeout = TimeSpan.FromSeconds(10) };
    try
    {
        var res = await http.GetAsync("/");
        Console.WriteLine($"GET / -> {(int)res.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Не удалось достучаться до приложения: {ex.Message}");
        return 1;
    }

    PerfSeeder.PrintK6Hint(attachUrl);
    return 0;
}

static void PrintStandInfo(string baseUrl, string appVersion, int postsCount)
{
    Console.WriteLine();
    Console.WriteLine("=== Стенд готов ===");
    Console.WriteLine($"URL:      {baseUrl}");
    Console.WriteLine($"Версия:   {appVersion}");
    Console.WriteLine($"Логин:    {UserConstants.TestUserUsername} / {UserConstants.TestUserPassword} (роль Admin)");
    Console.WriteLine($"Посты:    {PerfSeeder.PostSlugPrefix}0001..{PerfSeeder.PostSlugPrefix}{postsCount:D4} (+ helloworld)");
    Console.WriteLine("Страницы: / — статика; /posts — список из БД; /posts/{slug} — пост из БД");
}

static Task WaitUntilCancelled(TimeSpan? timeout)
{
    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    using var cts = timeout is null ? null : new CancellationTokenSource(timeout.Value);
    using var registration = cts?.Token.Register(() => tcs.TrySetResult());
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        tcs.TrySetResult();
    };

    Console.WriteLine();
    Console.WriteLine(timeout is null
        ? "Стенд работает. Остановка — Ctrl+C."
        : $"Стенд остановится сам через {timeout.Value.TotalMinutes:F0} мин или по Ctrl+C.");
    return tcs.Task;
}

namespace Mars.Performance.Stand
{
    public sealed record StandOptions
    {
        public int PostsCount { get; init; } = 1000;
        public string? AttachUrl { get; init; }
        public string? ReadyFile { get; init; }
        public int WaitMinutes { get; init; }
        public bool NoWait { get; init; }

        public static StandOptions Parse(string[] args)
        {
            var opts = new StandOptions();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--posts":
                        opts = opts with { PostsCount = int.Parse(args[++i]) };
                        break;
                    case "--attach":
                        opts = opts with { AttachUrl = args[++i] };
                        break;
                    case "--ready-file":
                        opts = opts with { ReadyFile = args[++i] };
                        break;
                    case "--wait-minutes":
                        opts = opts with { WaitMinutes = int.Parse(args[++i]) };
                        break;
                    case "--no-wait":
                        opts = opts with { NoWait = true };
                        break;
                    case "--help":
                    case "-h":
                        PrintUsage();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine($"Неизвестный аргумент: {args[i]}");
                        PrintUsage();
                        Environment.Exit(2);
                        break;
                }
            }
            return opts;
        }

        public static void PrintUsage() => Console.WriteLine("""
            Использование: Mars.Performance.Stand [опции]
              --posts <N>          количество постов для чтения (по умолчанию 1000)
              --attach <url>       не поднимать стенд — подсказка для уже запущенного приложения
              --ready-file <path>  записать URL готового стенда в файл (для run-perf.ps1)
              --wait-minutes <N>   автоостановка через N минут (по умолчанию — ждать Ctrl+C)
              --no-wait            поднять, проверить и сразу выйти (для CI/проверок)
            """);
    }
}
