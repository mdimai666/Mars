using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[AllExceptionCatchToUserActionResultFilter]
public class KpiController(IEnumerable<IKpiHandler> handlers) : ControllerBase
{
    const int MaxKeys = 50;

    [HttpGet]
    public async Task<Dictionary<string, KpiResult>> Get([FromQuery] string[] keys, CancellationToken cancellationToken)
    {
        // MVC биндит keys=a,b одним элементом (в отличие от minimal API) — режем запятые сами,
        // поддерживая и keys=a,b, и keys=a&keys=b
        var requested = keys.SelectMany(k => k.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            .Distinct()
                            .Take(MaxKeys)
                            .ToList();

        var byKey = handlers.GroupBy(h => h.Key).ToDictionary(g => g.Key, g => g.First());

        var tasks = requested.Select(key => Resolve(byKey, key, cancellationToken))
                             .Where(t => t is not null)
                             .Select(t => t!)
                             .ToList();

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r is not null).ToDictionary(r => r!.Key, r => r!);
    }

    /// <summary>Точное совпадение ключа; иначе — параметризованный ключ "{prefix}:{parameter}"
    /// для <see cref="IParameterizedKpiHandler"/> (например "posts.total:blog"). Неизвестные ключи игнорируются.</summary>
    static Task<KpiResult>? Resolve(Dictionary<string, IKpiHandler> byKey, string key, CancellationToken cancellationToken)
    {
        if (byKey.TryGetValue(key, out var handler))
            return handler.GetAsync(cancellationToken);

        var separator = key.IndexOf(':');
        if (separator > 0
            && byKey.TryGetValue(key[..separator], out var prefixed)
            && prefixed is IParameterizedKpiHandler parameterized)
        {
            return parameterized.GetAsync(key[(separator + 1)..], cancellationToken);
        }

        return null;
    }
}
