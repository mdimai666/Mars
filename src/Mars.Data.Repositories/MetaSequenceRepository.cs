using Mars.Cms.Abstractions.Repositories;
using Mars.Core.Exceptions;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Repositories;

internal class MetaSequenceRepository : IMetaSequenceRepository
{
    const int MaxAttempts = 5;

    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    public MetaSequenceRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<long> NextValueAsync(Guid metaFieldId, string scopeKey, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var row = await _marsDbContext.MetaSequences
                .FirstOrDefaultAsync(s => s.MetaFieldId == metaFieldId && s.ScopeKey == scopeKey, cancellationToken);

            if (row is null)
            {
                _marsDbContext.MetaSequences.Add(new MetaSequenceEntity
                {
                    Id = Guid.NewGuid(),
                    MetaFieldId = metaFieldId,
                    ScopeKey = scopeKey,
                    LastValue = 1,
                });

                try
                {
                    await _marsDbContext.SaveChangesAsync(cancellationToken);
                    return 1;
                }
                catch (DbUpdateException)
                {
                    // параллельная транзакция успела создать строку (уникальный индекс) — повтор с инкрементом
                    DetachTracked(metaFieldId, scopeKey);
                    continue;
                }
            }

            row.LastValue += 1;
            row.ModifiedAt = DateTimeOffset.Now;

            try
            {
                await _marsDbContext.SaveChangesAsync(cancellationToken);
                return row.LastValue;
            }
            catch (Exception ex) when (ex is ExpiredVersionTokenException or DbUpdateConcurrencyException)
            {
                // параллельный инкремент (конкурентный токен на LastValue) — перечитать и повторить
                _marsDbContext.Entry(row).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException($"cannot allocate sequence value for meta field '{metaFieldId}' scope '{scopeKey}'");
    }

    public async Task SetValueAsync(Guid metaFieldId, string scopeKey, long value, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var row = await _marsDbContext.MetaSequences
                .FirstOrDefaultAsync(s => s.MetaFieldId == metaFieldId && s.ScopeKey == scopeKey, cancellationToken);

            if (row is null)
            {
                _marsDbContext.MetaSequences.Add(new MetaSequenceEntity
                {
                    Id = Guid.NewGuid(),
                    MetaFieldId = metaFieldId,
                    ScopeKey = scopeKey,
                    LastValue = value,
                });

                try
                {
                    await _marsDbContext.SaveChangesAsync(cancellationToken);
                    return;
                }
                catch (DbUpdateException)
                {
                    // параллельная транзакция успела создать строку (уникальный индекс) — повтор с обновлением
                    DetachTracked(metaFieldId, scopeKey);
                    continue;
                }
            }

            row.LastValue = value;
            row.ModifiedAt = DateTimeOffset.Now;

            try
            {
                await _marsDbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is ExpiredVersionTokenException or DbUpdateConcurrencyException)
            {
                _marsDbContext.Entry(row).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException($"cannot set sequence value for meta field '{metaFieldId}' scope '{scopeKey}'");
    }

    void DetachTracked(Guid metaFieldId, string scopeKey)
    {
        var tracked = _marsDbContext.ChangeTracker.Entries<MetaSequenceEntity>()
            .FirstOrDefault(e => e.Entity.MetaFieldId == metaFieldId && e.Entity.ScopeKey == scopeKey);

        if (tracked is not null) tracked.State = EntityState.Detached;
    }

    /// <summary>
    /// Throws if this class has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Dispose the store
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }
}
