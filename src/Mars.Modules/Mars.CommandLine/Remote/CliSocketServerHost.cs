using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace Mars.CommandLine.Remote;

/// <summary>
/// Маркер соединения, пришедшего через UDS-эндпоинт: ставится connection-middleware
/// на ListenOptions, из HttpContext виден через Features (фичи соединения наследуются
/// HTTP-протоколом Kestrel).
/// </summary>
public sealed class CliUdsConnectionFeature
{
}

/// <summary>
/// Серверная сторона CLI-сокета (без привязки к Mars-специфике):
/// Kestrel-эндпоинт на UDS с маркером запросов и эндпоинты /_cli/ping + /_cli/exec.
/// </summary>
public static class CliSocketServerHost
{

    /// <summary>
    /// Переббиндит HTTP-адреса из Urls (явные Listen-эндпоинты отключают Urls в Kestrel)
    /// и добавляет UDS-эндпоинт: каждый запрос через него помечается маркером,
    /// по которому /_cli-эндпоинты отличают UDS-клиента от обычного HTTP-запроса.
    /// </summary>
    public static void ConfigureCliSocket(this KestrelServerOptions options, IConfiguration configuration, string socketPath, out CliUrlsPlan urlsPlan)
    {
        var urls = configuration[WebHostDefaults.ServerUrlsKey];
        if (string.IsNullOrWhiteSpace(urls))
        {
            // дефолты Kestrel: 8080 в контейнере, 5000 иначе
            urls = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
                ? "http://*:80"
                : "http://localhost:5000";
        }

        urlsPlan = CliUrlsParser.Parse(urls);
        foreach (var endpoint in urlsPlan.Endpoints)
        {
            switch (endpoint.Kind)
            {
                case CliUrlHostKind.Localhost:
                    options.ListenLocalhost(endpoint.Port);
                    break;
                case CliUrlHostKind.Any:
                    options.ListenAnyIP(endpoint.Port);
                    break;
                case CliUrlHostKind.Ip:
                    options.Listen(endpoint.Ip!, endpoint.Port);
                    break;
            }
        }

        options.ListenUnixSocket(socketPath, listen =>
        {
            listen.Use(next => connection =>
            {
                connection.Features.Set(new CliUdsConnectionFeature());
                return next(connection);
            });
        });
    }

    /// <summary>
    /// Мапит /_cli/ping и /_cli/exec. Эндпоинты видны и на HTTP-адресах, но любому
    /// запросу не через UDS возвращают 404: маркер ставится только per-endpoint
    /// middleware UDS-эндпоинта, подделать его из TCP-запроса нельзя.
    /// </summary>
    /// <param name="executor">Исполнение команды в рантайме сервера; TextWriter-ы стримят вывод клиенту.</param>
    public static void MapCliSocketEndpoints(this IEndpointRouteBuilder endpoints, CliServerInfo serverInfo, Func<string[], TextWriter, TextWriter, CancellationToken, Task<int>> executor)
    {
        endpoints.MapGet(MarsCliSocket.PingPath, (HttpContext ctx) =>
        {
            if (!IsUds(ctx)) return Results.NotFound();
            return Results.Json(serverInfo);
        });

        endpoints.MapPost(MarsCliSocket.ExecPath, async (HttpContext ctx) =>
        {
            if (!IsUds(ctx))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var request = await ctx.Request.ReadFromJsonAsync<CliExecRequest>(ctx.RequestAborted);
            if (request?.Args is null)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            if (request.ProtocolVersion != MarsCliSocket.ProtocolVersion)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsync(
                    $"unsupported protocol version {request.ProtocolVersion} (server supports {MarsCliSocket.ProtocolVersion})",
                    ctx.RequestAborted);
                return;
            }

            ctx.Response.ContentType = "application/x-ndjson";
            var outWriter = new CliFrameWriter(ctx.Response.Body, CliFrame.Out);
            var errWriter = new CliFrameWriter(ctx.Response.Body, CliFrame.Error);

            int exitCode;
            try
            {
                exitCode = await executor(request.Args, outWriter, errWriter, ctx.RequestAborted);
            }
            catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
            {
                return; // клиент отвалился (Ctrl+C) — exit-кадр писать некому
            }
            catch (Exception ex)
            {
                try
                {
                    await errWriter.SendAsync(CliFrame.Error, "mars cli: " + ex.Message + Environment.NewLine, null, CancellationToken.None);
                }
                catch (OperationCanceledException) { }
                exitCode = 1;
            }

            try
            {
                await outWriter.WriteExitAsync(exitCode, ctx.RequestAborted);
            }
            catch (OperationCanceledException) { }
        });
    }

    static bool IsUds(HttpContext ctx) => ctx.Features.Get<CliUdsConnectionFeature>() is not null;
}
