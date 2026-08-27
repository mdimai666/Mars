using Mars.Host.Shared.Models;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.WebSite;

/// <summary>
/// Обработчик запроса фронта — шаг пайплайна, который исполняется до статики фронтов
/// и fallback-рендера. Модули регистрируют свои обработчики в DI (AddSingleton),
/// не встраиваясь в код фронтов: пайплайн про них ничего не знает.
/// </summary>
public interface IFrontRequestHandler
{
    /// <summary>Меньшее значение исполняется раньше.</summary>
    int Order { get; }

    /// <returns>true — запрос обработан (short-circuit, дальше пайплайн не идёт); false — передать следующему.</returns>
    Task<bool> HandleAsync(HttpContext httpContext, MarsAppFront appFront, CancellationToken cancellationToken);
}
