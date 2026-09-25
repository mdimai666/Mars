namespace Mars.Identity.Abstractions.Dto.ApiKeys;

public record CreatedApiKeyDto : ApiKeySummary
{
    /// <summary>
    /// Полный ключ, показывается один раз при создании.
    /// </summary>
    public required string Key { get; init; }
}
