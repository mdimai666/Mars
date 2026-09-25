using Mars.Options.Abstractions.Services;
using Mars.Server.Contracts.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Mars.UseStartup.MarsParts;

/// <summary>
/// CORS-политика из опции БД <see cref="CorsOption"/> (динамически, на каждый запрос).
/// Дефолт (пустой список origins) — кросс-origin запросы запрещены: админка и фронты
/// работают same-origin. Явно перечисленным origins разрешаются куки (AllowCredentials).
/// </summary>
internal sealed class OptionCorsPolicyProvider(IOptionService optionService) : ICorsPolicyProvider
{
    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origins = optionService.GetOption<CorsOption>().AllowedOrigins;
        if (origins.Count == 0)
            return Task.FromResult<CorsPolicy?>(null);

        var policy = new CorsPolicyBuilder()
            .WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();

        return Task.FromResult<CorsPolicy?>(policy);
    }
}
