using System.Text.Json;
using FluentAssertions;
using Flurl.Http;
using Mars.DockerImage.Tests.Fixtures;
using Microsoft.AspNetCore.Http;

namespace Mars.DockerImage.Tests;

public class MarsStartTests : IClassFixture<MarsFixture>
{
    private const string AdminEmail = "admin@mail.ru";
    private const string AdminPassword = "Admin123!";

    // 1x1 PNG (67 байт) — заведомо валидный, лёгкий.
    private static readonly byte[] Png1X1 = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly MarsFixture _fixture;

    public MarsStartTests(MarsFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerContainerFact]
    public async Task MarsStart_EmptyDb_SucceedsAsync()
    {
        var req = await _fixture.Client.Request("/dev").AllowAnyHttpStatus().GetAsync();

        req.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Контракт non-root: главный процесс (dotnet) в контейнере работает под uid 1654.
    /// Ловит случайный возврат USER root в Dockerfile.
    /// </summary>
    [DockerContainerFact]
    public async Task Container_RunsAsNonRootUid1654Async()
    {
        // docker top требует колонку PID; uid — числовой эффективный id процесса.
        var (exitCode, output) = await MarsFixture.RunDockerAsync(
            "top", _fixture.MarsContainer.Id, "-o", "pid,uid,comm,args");

        exitCode.Should().Be(0, "docker top должен отработать; вывод: {0}", output);

        var dotnetRows = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 4 && parts[2].Contains("dotnet", StringComparison.Ordinal))
            .ToList();

        dotnetRows.Should().NotBeEmpty("в контейнере должен быть процесс dotnet");
        dotnetRows.Should().OnlyContain(parts => parts[1] == "1654",
            "процесс dotnet должен быть под uid 1654, а не root; строки: {0}",
            string.Join(" | ", dotnetRows.Select(p => string.Join(" ", p))));
    }

    /// <summary>
    /// Конфиг образа: USER 1654 и отсутствие DOTNET_SYSTEM_GLOBALIZATION_INVARIANT
    /// (иначе это chiseled без ICU — сломана локализация ru/en).
    /// </summary>
    [DockerContainerFact]
    public async Task Image_Config_NonRootUser_NoInvariantGlobalizationAsync()
    {
        var (exitCode, output) = await MarsFixture.RunDockerAsync(
            "inspect", _fixture.MarsContainer.Id, "--format", "{{.Config.User}}|{{json .Config.Env}}");

        exitCode.Should().Be(0, "docker inspect должен отработать; вывод: {0}", output);

        var parts = output.Split('|', 2, StringSplitOptions.TrimEntries);
        parts[0].Should().Be("1654", "в образе должен быть задан USER 1654");
        parts[1].Should().NotContain("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT",
            "chiseled-extra идёт с ICU — инвариантная глобализация не должна быть включена");
    }

    /// <summary>
    /// В логах контейнера после старта не должно быть необработанных исключений
    /// (падение нативных кодеков/миграций/прав всплыло бы сюда).
    /// </summary>
    [DockerContainerFact]
    public async Task Container_Logs_HaveNoUnhandledErrorsAsync()
    {
        var (exitCode, output) = await MarsFixture.RunDockerAsync(
            "logs", _fixture.MarsContainer.Id);

        exitCode.Should().Be(0);
        output.Should().NotContain("Unhandled", "в логах не должно быть необработанных исключений");
    }

    /// <summary>
    /// HTTP-смоук по подсистемам: API-pipeline (валидация тела логина → 400),
    /// статика WASM-клиента, 404 на неизвестный путь (роутинг не «глотает»).
    /// Swagger в Production не маппится — не проверяем.
    /// </summary>
    [DockerContainerFact]
    public async Task Http_ApiValidation_Static_And_UnknownPathAsync()
    {
        var emptyLogin = await _fixture.Client.Request("/api/account/login")
            .AllowAnyHttpStatus().PostJsonAsync(new { });
        emptyLogin.StatusCode.Should().Be(StatusCodes.Status400BadRequest,
            "пустое тело логина должно отклоняться валидацией (API-pipeline жив)");

        var wasmStatic = await _fixture.Client.Request("/dev/_framework/blazor.webassembly.js")
            .AllowAnyHttpStatus().GetAsync();
        wasmStatic.StatusCode.Should().Be(StatusCodes.Status200OK, "статика WASM-клиента должна раздаваться");

        var unknown = await _fixture.Client.Request($"/no-such-page-{Guid.NewGuid():N}").AllowAnyHttpStatus().GetAsync();
        unknown.StatusCode.Should().Be(StatusCodes.Status404NotFound, "неизвестный путь должен давать 404");
    }

    /// <summary>
    /// Логин админа работает (cookie-вход задействует DataProtection), и ключи DP
    /// реально создаются в /app/.aspnet — не-root пишет в $HOME.
    /// </summary>
    [DockerContainerFact]
    public async Task Login_DataProtectionKeys_CreatedInAppAspnetAsync()
    {
        var token = await LoginAndGetTokenAsync();

        token.Should().NotBeNullOrEmpty("логин дефолтного админа должен вернуть токен");

        var keysFound = await WaitForDataProtectionKeysAsync(TimeSpan.FromSeconds(40));
        keysFound.Should().BeTrue("ключи DataProtection должны появиться в /app/.aspnet/DataProtection-Keys");
    }

    /// <summary>
    /// Сквозной медиа-флоу: upload PNG → файл сохранён (EF + диск) и отдаётся по URL.
    /// Покрывает запись не-root в wwwroot/upload и раздачу статики.
    /// </summary>
    [DockerContainerFact]
    public async Task Media_UploadFile_IsStoredAndServedAsync()
    {
        var token = await LoginAndGetTokenAsync();

        using var content = new MemoryStream(Png1X1);
        var upload = await _fixture.Client.Request("/api/media/Upload")
            .WithOAuthBearerToken(token)
            .PostMultipartAsync(mp => mp.AddFile("file", content, "mars-test.png", "image/png"));

        upload.StatusCode.Should().Be(StatusCodes.Status200OK, $"upload должен пройти: {await upload.GetStringAsync()}");

        using var json = JsonDocument.Parse(await upload.GetStringAsync());
        var id = GetPropertyCaseInsensitive(json.RootElement, "id");
        var urlRelative = GetPropertyCaseInsensitive(json.RootElement, "urlRelative");

        id.ValueKind.Should().Be(JsonValueKind.String, "ответ должен содержать id файла");
        Guid.TryParse(id.GetString(), out _).Should().BeTrue();
        urlRelative.ValueKind.Should().Be(JsonValueKind.String, "ответ должен содержать urlRelative");
        var fileUrl = urlRelative.GetString();
        fileUrl.Should().NotBeNullOrEmpty();

        var download = await _fixture.Client.Request(fileUrl!).AllowAnyHttpStatus().GetAsync();
        download.StatusCode.Should().Be(StatusCodes.Status200OK, "загруженный файл должен отдаваться по URL");

        await using var body = await download.GetStreamAsync();
        using var buffer = new MemoryStream();
        await body.CopyToAsync(buffer);

        // Upload прогоняет изображение через MagicScaler (нативные кодеки на chiseled!),
        // поэтому размер файла может отличаться от исходного — проверяем сигнатуру PNG.
        var bytes = buffer.ToArray();
        bytes.Length.Should().BeGreaterThan(8, "по URL должен отдаваться файл изображения");
        var hex = Convert.ToHexString(bytes.Take(16).ToArray());
        (bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G')
            .Should().BeTrue($"файл должен быть PNG (магия 89 50 4E 47), первые байты: {hex}");
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

    private async Task<bool> WaitForDataProtectionKeysAsync(TimeSpan timeout)
    {
        var containerId = _fixture.MarsContainer.Id;
        var targetDir = Path.Combine(Path.GetTempPath(), $"mars-dp-{Guid.NewGuid():N}");

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var (exitCode, _) = await MarsFixture.RunDockerAsync(
                "cp", $"{containerId}:/app/.aspnet/DataProtection-Keys", targetDir);

            if (exitCode == 0 && Directory.Exists(targetDir)
                && Directory.EnumerateFiles(targetDir, "*.xml").Any())
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return false;
    }
}
