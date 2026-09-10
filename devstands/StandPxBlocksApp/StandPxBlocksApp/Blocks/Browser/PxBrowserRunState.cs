using Microsoft.Playwright;

namespace StandPxBlocksApp.Blocks.Browser;

/// <summary>
/// Состояние запуска контекста «browser»: Playwright и системный Edge
/// (channel msedge, видимое окно — как Mars.E2E.Tests.BaseE2ETests), одна
/// вкладка на запуск. Создаётся PxRunController-ом, передаётся во владение
/// IPxRunManager (диспозится по завершении исполнения) и попадает в
/// имплементации браузерных блоков конструктором. Запуск браузера — ленивый:
/// если программа не использует ни одного браузерного блока, Edge не стартует.
/// </summary>
public sealed class PxBrowserRunState : IAsyncDisposable
{
    /// <summary>Таймаут действий Playwright по умолчанию (навигация, ожидания, клики), мс.</summary>
    public const float DefaultTimeoutMs = 15_000;

    private readonly object _gate = new();
    private Task<IPage>? _pageTask;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;

    /// <summary>Вкладка запуска; при первом обращении запускает Playwright и Edge.</summary>
    public Task<IPage> GetPageAsync()
    {
        lock (_gate)
            return _pageTask ??= CreatePageAsync();
    }

    private async Task<IPage> CreatePageAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge", // системный Edge из Windows 11
            Headless = false,
            SlowMo = 50
        });
        _context = await _browser.NewContextAsync();
        _context.SetDefaultTimeout(DefaultTimeoutMs);
        return await _context.NewPageAsync();
    }

    /// <summary>Закрыть браузер и освободить Playwright; ошибки уборки не пробрасываем.</summary>
    public async ValueTask DisposeAsync()
    {
        Task<IPage>? pageTask;
        lock (_gate)
        {
            pageTask = _pageTask;
            _pageTask = null;
        }

        if (pageTask == null)
            return;

        try
        {
            await pageTask; // дождаться, если браузер ещё стартует
        }
        catch
        {
            // Старт не удался — закрывать нечего.
        }

        try
        {
            if (_context != null)
                await _context.CloseAsync();
        }
        catch
        {
        }

        try
        {
            if (_browser != null)
                await _browser.CloseAsync();
        }
        catch
        {
        }

        try
        {
            _playwright?.Dispose();
        }
        catch
        {
        }
    }
}
