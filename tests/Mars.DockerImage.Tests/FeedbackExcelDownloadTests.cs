using System.Text.Json;
using ClosedXML.Excel;
using FluentAssertions;
using Flurl.Http;
using Mars.DockerImage.Tests.Fixtures;
using Microsoft.AspNetCore.Http;

namespace Mars.DockerImage.Tests;

/// <summary>
/// Генерация Excel-отчётов ClosedXML.Report на реальном прод-артефакте:
/// Linux chiseled-образ, dotnet publish -c Release. Исторически генерация
/// ломалась именно на Linux из-за шрифтов — Windows-прогоны это не ловят.
/// </summary>
[Collection(MarsCollection.Name)]
public class FeedbackExcelDownloadTests
{
    private const string AdminEmail = "admin@mail.ru";
    private const string AdminPassword = "Admin123!";

    private readonly MarsFixture _fixture;

    public FeedbackExcelDownloadTests(MarsFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Сквозной прод-путь: создание фидбека (anonymous) → логин админа →
    /// GET /api/Feedback/DownloadExcel → валидный xlsx из шаблона
    /// Res/Excel/Feedback/feedback_list.xlsx (Content Include="Res\**" в образе).
    /// </summary>
    [DockerContainerFact]
    public async Task Feedback_DownloadExcel_GeneratesValidXlsxAsync()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];

        var create = await _fixture.Client.Request("/api/Feedback")
            .AllowAnyHttpStatus()
            .PostJsonAsync(new
            {
                title = $"Docker Excel тест {marker}",
                phone = "+7 900 123-45-67",
                email = $"docker-{marker}@example.com",
                type = "InfoMessage",
                filledUsername = "docker-test",
                content = $"Содержимое письма для Excel-теста {marker}",
            });

        create.StatusCode.Should().Be(StatusCodes.Status201Created,
            $"фидбек должен создаться: {await create.GetStringAsync()}");

        var token = await LoginAndGetTokenAsync();

        var download = await _fixture.Client.Request("/api/Feedback/DownloadExcel")
            .WithOAuthBearerToken(token)
            .AllowAnyHttpStatus()
            .GetAsync();

        download.StatusCode.Should().Be(StatusCodes.Status200OK,
            "DownloadExcel должен ответить 200 (падение генерации/шрифтов/пути к шаблону дало бы 500)");

        await using var body = await download.GetStreamAsync();
        using var ms = new MemoryStream();
        await body.CopyToAsync(ms);
        var bytes = ms.ToArray();

        bytes.Length.Should().BeGreaterThan(1000, "xlsx должен быть непустым");
        (bytes[0] == (byte)'P' && bytes[1] == (byte)'K')
            .Should().BeTrue("xlsx — это zip (магия PK)");

        ms.Position = 0;
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        ws.Cell("A1").GetString().Should().Be("Письма");

        var cells = ws.RangeUsed()?.CellsUsed().Select(c => c.GetFormattedString()).ToList()
                    ?? new List<string>();
        cells.Should().Contain(s => s.Contains(marker), "созданный фидбек должен попасть в отчёт");
        cells.Should().NotContain(s => s.Contains("{{"), "плейсхолдеры шаблона должны быть заменены");
    }

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _fixture.Client.Request("/api/account/login")
            .PostJsonAsync(new { login = AdminEmail, password = AdminPassword });

        response.StatusCode.Should().Be(StatusCodes.Status200OK,
            $"логин должен пройти: {await response.GetStringAsync()}");

        using var json = JsonDocument.Parse(await response.GetStringAsync());
        var token = GetPropertyCaseInsensitive(json.RootElement, "token");
        return token.ValueKind == JsonValueKind.String ? token.GetString() ?? "" : "";
    }

    private static JsonElement GetPropertyCaseInsensitive(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return default;
    }
}
