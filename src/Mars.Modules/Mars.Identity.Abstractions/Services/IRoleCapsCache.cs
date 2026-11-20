namespace Mars.Identity.Abstractions.Services;

/// <summary>
/// Singleton-снимок «имя роли → набор прав» (ClaimType = RoleCaps.ClaimType) из БД.
/// Проверка прав идёт через кэш, а не через клеймы в токене — смена прав действует
/// сразу, без релогина. Инвалидация: при SaveRoleClaims и любом Create/Update/Delete
/// роли (rename меняет ключ снимка).
/// </summary>
public interface IRoleCapsCache
{
    Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetRoleCaps(CancellationToken cancellationToken = default);
    void Invalidate();
}
