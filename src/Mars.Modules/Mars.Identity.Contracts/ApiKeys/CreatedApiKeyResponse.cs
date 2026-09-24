namespace Mars.Identity.Contracts.ApiKeys;

public record CreatedApiKeyResponse : ApiKeySummaryResponse
{
    /// <summary>
    /// Полный ключ, показывается один раз при создании.
    /// </summary>
    public required string Key { get; init; }
}
