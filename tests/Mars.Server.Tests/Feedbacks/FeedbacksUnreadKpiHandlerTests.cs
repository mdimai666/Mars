using FluentAssertions;
using Mars.Cms.Contracts.Feedbacks;
using Mars.Cms.Host.Kpi;
using Mars.Data.Constants;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.InMemory;
using Mars.Data.Options;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Mars.Server.Managers;
using Microsoft.Extensions.Caching.Memory;

namespace Mars.Server.Tests.Feedbacks;

public class FeedbacksUnreadKpiHandlerTests : IDisposable
{
    private readonly MarsDbContextInMemoryFactory _factory;
    private readonly MemoryCache _cache;
    private readonly EventManager _eventManager;
    private readonly FeedbacksUnreadKpiHandler _handler;

    public FeedbacksUnreadKpiHandlerTests()
    {
        _factory = new MarsDbContextInMemoryFactory(new DatabaseConnectionOpt
        {
            ConnectionString = $"{DatabaseProviderConstants.InMemoryDb}:{Guid.NewGuid()}",
            ProviderName = DatabaseProviderConstants.InMemoryDb,
        });
        _cache = new MemoryCache(new MemoryCacheOptions());
        _eventManager = new EventManager();
        _handler = new FeedbacksUnreadKpiHandler(_factory, _cache, _eventManager);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private MarsDbContext CreateContext() => _factory.CreateInstance();

    private async Task<Guid> SeedFeedback(bool isRead)
    {
        var entity = new FeedbackEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Title = "Test feedback",
            IsRead = isRead,
        };
        await using var context = CreateContext();
        context.Feedbacks.Add(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    [Fact]
    public async Task GetAsync_CountsOnlyUnread()
    {
        // Arrange
        await SeedFeedback(isRead: false);
        await SeedFeedback(isRead: false);
        await SeedFeedback(isRead: true);

        // Act
        var result = await _handler.GetAsync(CancellationToken.None);

        // Assert
        result.Key.Should().Be(FeedbackKpiKeys.Unread);
        result.Value.Should().Be(2);
        result.Label.Should().Be(FeedbackKpiKeys.Unread);
    }

    [Fact]
    public async Task GetAsync_ValueIsCached()
    {
        // Arrange
        await SeedFeedback(isRead: false);
        var first = await _handler.GetAsync(CancellationToken.None);

        // Act — добавляем непрочитанное БЕЗ события: кэш не должен обновиться
        await SeedFeedback(isRead: false);
        var second = await _handler.GetAsync(CancellationToken.None);

        // Assert
        first.Value.Should().Be(1);
        second.Value.Should().Be(1);
    }

    [Fact]
    public async Task FeedbackReadEvent_InvalidatesCache()
    {
        // Arrange
        var id = await SeedFeedback(isRead: false);
        await SeedFeedback(isRead: false);
        (await _handler.GetAsync(CancellationToken.None)).Value.Should().Be(2);

        // Act — помечаем прочитанным и шлём событие (как FeedbackService.MarkRead)
        await using (var context = CreateContext())
        {
            var entity = await context.Feedbacks.FindAsync(id);
            entity!.IsRead = true;
            await context.SaveChangesAsync();
        }
        _eventManager.TriggerEvent(new ManagerEventPayload(_eventManager.Defaults.FeedbackRead(), new { Id = id }));

        // Assert
        (await _handler.GetAsync(CancellationToken.None)).Value.Should().Be(1);
    }

    [Fact]
    public async Task FeedbackAddEvent_InvalidatesCache()
    {
        // Arrange
        await SeedFeedback(isRead: false);
        (await _handler.GetAsync(CancellationToken.None)).Value.Should().Be(1);

        // Act
        await SeedFeedback(isRead: false);
        _eventManager.TriggerEvent(new ManagerEventPayload(_eventManager.Defaults.FeedbackAdd(), new { }));

        // Assert
        (await _handler.GetAsync(CancellationToken.None)).Value.Should().Be(2);
    }

    [Fact]
    public async Task FeedbackDeleteEvent_InvalidatesCache()
    {
        // Arrange
        var id = await SeedFeedback(isRead: false);
        await SeedFeedback(isRead: true);
        (await _handler.GetAsync(CancellationToken.None)).Value.Should().Be(1);

        // Act
        await using (var context = CreateContext())
        {
            var entity = await context.Feedbacks.FindAsync(id);
            context.Feedbacks.Remove(entity!);
            await context.SaveChangesAsync();
        }
        _eventManager.TriggerEvent(new ManagerEventPayload(_eventManager.Defaults.FeedbackDelete(), new { Id = id }));

        // Assert
        (await _handler.GetAsync(CancellationToken.None)).Value.Should().Be(0);
    }
}