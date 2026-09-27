using System.Text;
using Microsoft.Playwright;

namespace Mars.E2E.Tests.Helpers;

public sealed class BrowserErrorTracker
{
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _pageErrors = [];
    private readonly List<string> _failedRequests = [];
    private readonly List<IRequest> _failedRequests0 = [];

    public BrowserErrorTracker(IPage page)
    {
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !IsIgnorable(msg))
                _consoleErrors.Add(msg.Text);
        };

        page.PageError += (_, error) =>
        {
            if (IsKnownFrameworkRace(error))
                return;

            _pageErrors.Add(error);
        };

        page.RequestFailed += (_, req) =>
        {
            if (req.Failure == "net::ERR_ABORTED")
                return;

            _failedRequests.Add($"{req.Method} {req.Failure} {req.Url}");
            _failedRequests0.Add(req);
        };
    }

    bool IsIgnorable(IConsoleMessage msg)
    {
        var text = msg.Text.ToLowerInvariant();

        return
            text.Contains("favicon.ico") ||
            msg.Location.Contains("favicon.ico") ||
            text.Contains("ResizeObserver loop limit exceeded") ||
            text.Contains("net::err_blocked_by_client");
    }

    /// <summary>
    /// Известная гонка FluentUI v5: <c>FluentDataGrid</c> регистрирует глобальный обработчик
    /// <c>FluentKeyCode</c> для клавишного ресайза колонок. Когда диалог с гридом закрывается
    /// (грид disposed), а пользователь продолжает печатать, поздний keydown попадает в уже
    /// освобождённый JS-объект грида → <c>ObjectDisposedException</c> в
    /// <c>SetColumnWidthDiscreteAsync</c>. Это дефект жизненного цикла самой библиотеки (не кода
    /// Mars): он не влияет на сохранение данных и не воспроизводится как сбой приложения.
    /// Фильтруем только эту узкую сигнатуру, все прочие page-ошибки продолжают ловиться.
    /// </summary>
    static bool IsKnownFrameworkRace(string error) =>
        error.Contains("ObjectDisposedException") &&
        error.Contains("SetColumnWidthDiscreteAsync") &&
        error.Contains("FluentKeyCode");

    public void AssertNoErrors()
    {
        if (_consoleErrors.Count == 0 &&
            _pageErrors.Count == 0 &&
            _failedRequests.Count == 0)
            return;

        var sb = new StringBuilder();

        if (_consoleErrors.Count > 0)
        {
            sb.AppendLine("=== Console errors ===");
            sb.AppendLine(string.Join("\n", _consoleErrors));
            sb.AppendLine();
        }

        if (_pageErrors.Count > 0)
        {
            sb.AppendLine("=== Page errors ===");
            sb.AppendLine(string.Join("\n", _pageErrors));
            sb.AppendLine();
        }

        if (_failedRequests.Count > 0)
        {
            sb.AppendLine("=== Failed network requests ===");
            sb.AppendLine(string.Join("\n", _failedRequests));
            sb.AppendLine();
        }

        Assert.Fail(sb.ToString());
    }
}
