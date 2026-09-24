using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.ApiKeys;
using Mars.Identity.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Repositories;

internal class UserApiKeyRepository : IUserApiKeyRepository
{
    private readonly MarsDbContext _marsDbContext;

    public UserApiKeyRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<ApiKeyValidationDto?> GetForValidation(Guid keyId, CancellationToken cancellationToken)
    {
        var entity = await _marsDbContext.UserApiKeys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == keyId, cancellationToken);

        return entity is null ? null : new ApiKeyValidationDto
        {
            UserId = entity.UserId,
            KeyHash = entity.KeyHash,
            ExpiresAt = entity.ExpiresAt,
        };
    }

    public async Task<IReadOnlyCollection<ApiKeySummary>> ListByUser(Guid userId, CancellationToken cancellationToken)
    {
        var entities = await _marsDbContext.UserApiKeys.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToSummary).ToList();
    }

    public async Task Create(CreateApiKeyQuery query, Guid id, string keyHash, string keyPrefix, CancellationToken cancellationToken)
    {
        var entity = new UserApiKeyEntity
        {
            Id = id,
            UserId = query.UserId,
            Name = query.Name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            ExpiresAt = query.ExpiresAt,
        };

        await _marsDbContext.UserApiKeys.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountByUser(Guid userId, CancellationToken cancellationToken)
        => _marsDbContext.UserApiKeys.CountAsync(s => s.UserId == userId, cancellationToken);

    public async Task<bool> Delete(Guid id, Guid userId, CancellationToken cancellationToken)
        => await _marsDbContext.UserApiKeys.Where(s => s.Id == id && s.UserId == userId).ExecuteDeleteAsync(cancellationToken) > 0;

    private static ApiKeySummary ToSummary(UserApiKeyEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        KeyPrefix = entity.KeyPrefix,
        CreatedAt = entity.CreatedAt,
        ExpiresAt = entity.ExpiresAt,
    };
}
