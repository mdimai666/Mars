namespace Mars.Shared.Contracts.PostTypes;

public record PostTypePresentationResponse
{
    /// <summary>
    /// Относительный путь к шаблону списка во фронте админки (data/admin/front).
    /// </summary>
    public required string? ListViewTemplate { get; init; }
}
