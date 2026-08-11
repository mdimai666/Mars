using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Проверяет, что уровень логирования в файл подхватывается из конфигурации без перезапуска
/// (воспроизводит регистрацию из MarsStartupPartLogging: AddConfiguration + AddFile).
/// </summary>
public class LoggingHotReloadTests : IDisposable
{
    readonly string dir = Path.Combine(Path.GetTempPath(), "mars-log-hotreload-tests", Guid.NewGuid().ToString("N"));

    string ConfigPath => Path.Combine(dir, "appsettings.json");
    string LogPath => Path.Combine(dir, "app.log");

    void WriteConfig(string fileLevel)
    {
        Directory.CreateDirectory(dir);
        var json = $$"""
        {
            "Logging": {
                "LogLevel": { "Default": "Information" },
                "File": { "LogLevel": { "Default": "{{fileLevel}}" } }
            }
        }
        """;
        // атомарная замена: reader конфигурации не видит частично записанный файл
        var tmp = ConfigPath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, ConfigPath, overwrite: true);
    }

    string ReadLog()
    {
        if (!File.Exists(LogPath)) return "";
        // NReco держит файл открытым — читаем с ReadWrite, как LogFileFilter.ReadSeamless
        using var fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        return sr.ReadToEnd();
    }

    static async Task WaitUntilAsync(Func<bool> condition, string because, int timeoutMs = 15000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            await Task.Delay(100);

        condition().Should().BeTrue(because);
    }

    [Fact]
    public async Task FileLogLevel_ChangedInConfig_AppliesWithoutRestart()
    {
        WriteConfig("Warning");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = dir });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(ConfigPath, optional: false, reloadOnChange: true);

        builder.Services.AddLogging(lb =>
        {
            lb.AddConfiguration(builder.Configuration);
            lb.AddFile(LogPath, _ => { });
        });

        var app = builder.Build();
        try
        {
            var logger = app.Services.GetRequiredService<ILogger<LoggingHotReloadTests>>();

            logger.LogWarning("warn-before");
            logger.LogInformation("info-before");

            await WaitUntilAsync(() => ReadLog().Contains("warn-before"), "NReco пишет файл асинхронно");
            await Task.Delay(300);
            ReadLog().Should().NotContain("info-before");

            // меняем уровень без перезапуска; повторяем запись файла,
            // пока reload не подхватит (доп. FS-события на случай пропуска)
            var sw = Stopwatch.StartNew();
            while (builder.Configuration["Logging:File:LogLevel:Default"] != "Information" && sw.ElapsedMilliseconds < 15000)
            {
                WriteConfig("Information");
                await Task.Delay(500);
            }
            builder.Configuration["Logging:File:LogLevel:Default"].Should().Be("Information");

            logger.LogInformation("info-after");

            await WaitUntilAsync(() => ReadLog().Contains("info-after"), "уровень из конфигурации применился без перезапуска");
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    public void Dispose()
    {
        try
        {
            var baseDir = Path.GetDirectoryName(dir)!;
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, true);
        }
        catch
        {
            // временная папка
        }
    }
}
