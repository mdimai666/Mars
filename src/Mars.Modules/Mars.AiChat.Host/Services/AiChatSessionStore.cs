using Mars.AiChat.Abstractions.Interfaces;
using Mars.AiChat.Abstractions.Models;
using Mars.AiChat.Contracts.Dto;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Mars.AiChat.Host.Services;

/// <summary>
/// Хранение чатов в HybridCache (L2 — Postgres distributed cache).
/// </summary>
public class AiChatSessionStore : IAiChatSessionStore
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        LocalCacheExpiration = TimeSpan.FromHours(1),
        Expiration = TimeSpan.FromDays(7),
    };

    private static readonly string[] Tags = ["aichat"];

    private readonly HybridCache _cache;
    private readonly ILogger<AiChatSessionStore> _logger;
    private readonly SemaphoreSlim _indexLock = new(1, 1);

    public AiChatSessionStore(HybridCache cache, ILogger<AiChatSessionStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private record SessionCacheItem(AiChatSessionState? Session);

    static string SessionKey(Guid userId, Guid chatId) => $"aichat:session:{userId}:{chatId}";
    static string IndexKey(Guid userId) => $"aichat:index:{userId}";

    public async Task<AiChatSessionState?> GetAsync(Guid chatId, Guid userId, CancellationToken ct = default)
    {
        var item = await _cache.GetOrCreateAsync(SessionKey(userId, chatId),
            _ => new ValueTask<SessionCacheItem>(new SessionCacheItem(null)), EntryOptions, Tags, ct);

        return item.Session;
    }

    public async Task SaveAsync(AiChatSessionState state, CancellationToken ct = default)
    {
        state.ModifiedAtUtc = DateTime.UtcNow;

        await _cache.SetAsync(SessionKey(state.UserId, state.Id),
            new SessionCacheItem(state), EntryOptions, Tags, ct);

        await UpdateIndexAsync(state.UserId, list =>
        {
            list.RemoveAll(s => s.Id == state.Id);
            list.Insert(0, state.ToSummary(isRunning: false));
        }, ct);
    }

    public async Task DeleteAsync(Guid chatId, Guid userId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(SessionKey(userId, chatId), ct);
        await UpdateIndexAsync(userId, list => list.RemoveAll(s => s.Id == chatId), ct);
    }

    public async Task<IReadOnlyList<AiChatSessionSummary>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _cache.GetOrCreateAsync(IndexKey(userId),
            _ => new ValueTask<List<AiChatSessionSummary>>([]), EntryOptions, Tags, ct);

        return list;
    }

    private async Task UpdateIndexAsync(Guid userId, Action<List<AiChatSessionSummary>> mutate, CancellationToken ct)
    {
        await _indexLock.WaitAsync(ct);
        try
        {
            var list = await _cache.GetOrCreateAsync(IndexKey(userId),
                _ => new ValueTask<List<AiChatSessionSummary>>([]), EntryOptions, Tags, ct);

            mutate(list);

            await _cache.SetAsync(IndexKey(userId), list, EntryOptions, Tags, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiChat: failed to update session index for user {UserId}", userId);
        }
        finally
        {
            _indexLock.Release();
        }
    }
}
