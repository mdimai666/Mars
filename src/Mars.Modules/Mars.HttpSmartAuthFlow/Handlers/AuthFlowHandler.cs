using Mars.HttpSmartAuthFlow.Exceptions;
using Mars.HttpSmartAuthFlow.Strategies;
using Microsoft.Extensions.Logging;

namespace Mars.HttpSmartAuthFlow.Handlers;

public sealed class AuthFlowHandler : DelegatingHandler
{
    private readonly IAuthStrategy _strategy;
    private readonly AuthFlowHandlerOptions _options;
    private readonly ILogger? _logger;

    public AuthFlowHandler(IAuthStrategy strategy, AuthFlowHandlerOptions? options = null, ILogger? logger = null)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _options = options ?? new AuthFlowHandlerOptions();
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Инициализируем контекст запроса
        var context = new AuthFlowHandlerContext
        {
            Request = request,
            Strategy = _strategy,
            Attempt = 1,
            MaxAttempts = _options.MaxRetryAttempts,
            StartTime = DateTimeOffset.UtcNow,
            ConfigId = _strategy.Config.Id
        };

        // Добавляем заголовок для отслеживания
        //request.Headers.Add("X-Auth-Attempt", context.Attempt.ToString());

        try
        {
            var response = await SendWithRetryAsync(context, cancellationToken);

            // Записываем метрики успешного запроса
            var duration = DateTimeOffset.UtcNow - context.StartTime;
            _logger?.LogDebug(
                "[{ConfigId}] Request completed in {DurationMs}ms after {Attempts} attempt(s)",
                context.ConfigId,
                duration.TotalMilliseconds,
                context.Attempt);

            return response;
        }
        catch (Exception ex)
        {
            // Записываем метрики неудачного запроса
            _logger?.LogError(
                ex,
                "[{ConfigId}] Request failed after {Attempts} attempt(s)",
                context.ConfigId,
                context.Attempt);

            throw;
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        AuthFlowHandlerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Attempt > context.MaxAttempts)
        {
            _logger?.LogWarning(
                "[{ConfigId}] Max retry attempts ({Max}) exceeded for {Method} {Uri}",
                context.ConfigId,
                context.MaxAttempts,
                context.Request.Method,
                context.Request.RequestUri);

            throw new AuthenticationException(
                $"Authentication failed after {context.MaxAttempts} attempts. " +
                $"Last error: {context.LastError}");
        }

        try
        {
            // Применяем аутентификацию
            await _strategy.ApplyAuthenticationAsync(context.Request);

            _logger?.LogDebug(
                "[{ConfigId}] Attempt #{Attempt}/{Max}: {Method} {Uri}",
                context.ConfigId,
                context.Attempt,
                context.MaxAttempts,
                context.Request.Method,
                context.Request.RequestUri);

            var response = await base.SendAsync(context.Request, cancellationToken);

            _logger?.LogDebug(
                "[{ConfigId}] Response: {StatusCode} (Attempt #{Attempt})",
                context.ConfigId,
                (int)response.StatusCode,
                context.Attempt);

            // Проверяем, нужно ли перелогиниться
            if (ShouldReauthenticate(response))
            {
                _logger?.LogInformation(
                    "[{ConfigId}] {StatusCode} received - reauthenticating (Attempt #{Attempt}/{Max})",
                    context.ConfigId,
                    (int)response.StatusCode,
                    context.Attempt,
                    context.MaxAttempts);

                var canRetry = await _strategy.HandleUnauthorizedAsync(context.Request);

                if (!canRetry)
                {
                    _logger?.LogWarning(
                        "[{ConfigId}] Strategy does not support reauthentication",
                        context.ConfigId);

                    return response;
                }

                // Готовимся к повторной попытке
                context.Attempt++;
                context.LastResponse = response;
                context.LastError = $"Status code {(int)response.StatusCode}";

                // Обновляем заголовок попытки
                //if (context.Request.Headers.Contains("X-Auth-Attempt"))
                //{
                //    context.Request.Headers.Remove("X-Auth-Attempt");
                //}
                //context.Request.Headers.Add("X-Auth-Attempt", context.Attempt.ToString());

                // Клонируем запрос
                context.Request = await CloneRequestAsync(context.Request, cancellationToken);

                // Рекурсивный вызов
                return await SendWithRetryAsync(context, cancellationToken);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogInformation("[{ConfigId}] Request cancelled", context.ConfigId);
            throw;
        }
        catch (Exception ex)
        {
            context.LastError = ex.Message;

            _logger?.LogError(
                ex,
                "[{ConfigId}] Error on attempt #{Attempt}: {ErrorMessage}",
                context.ConfigId,
                context.Attempt,
                ex.Message);

            if (ShouldRetryOnException(ex) && context.Attempt < context.MaxAttempts)
            {
                _logger?.LogInformation(
                    "[{ConfigId}] Retrying after exception (Attempt #{Attempt}/{Max})",
                    context.ConfigId,
                    context.Attempt,
                    context.MaxAttempts);

                await _strategy.InvalidateAsync();
                context.Request = await CloneRequestAsync(context.Request, cancellationToken);
                context.Attempt++;

                return await SendWithRetryAsync(context, cancellationToken);
            }

            throw;
        }
    }

    private bool ShouldReauthenticate(HttpResponseMessage response)
    {
        return _options.UnauthorizedStatusCodes.Contains(response.StatusCode);
    }

    private bool ShouldRetryOnException(Exception ex)
    {
        return _options.RetryableExceptions.Any(type => type.IsInstanceOfType(ex));
    }

    private async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Копируем заголовки запроса
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Копируем содержимое
        if (request.Content != null)
        {
            var buffer = new MemoryStream();
            await request.Content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            clone.Content = new StreamContent(buffer);

            // Копируем заголовки содержимого
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _strategy is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[{ConfigId}] Error disposing strategy", _strategy.Config.Id);
            }
        }

        base.Dispose(disposing);
    }
}
