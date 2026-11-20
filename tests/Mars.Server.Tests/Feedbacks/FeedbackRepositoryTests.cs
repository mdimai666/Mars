using FluentAssertions;
using Mars.Cms.Abstractions.Dto.Feedbacks;
using Mars.Core.Exceptions;
using Mars.Data.Constants;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.InMemory;
using Mars.Data.Options;
using Mars.Data.OwnedTypes.Feedbacks;
using Mars.Data.Repositories;

namespace Mars.Server.Tests.Feedbacks;

public class FeedbackRepositoryTests
{
    private readonly MarsDbContextInMemoryFactory _factory;

    public FeedbackRepositoryTests()
    {
        _factory = new MarsDbContextInMemoryFactory(new DatabaseConnectionOpt
        {
            ConnectionString = $"{DatabaseProviderConstants.InMemoryDb}:{Guid.NewGuid()}",
            ProviderName = DatabaseProviderConstants.InMemoryDb,
        });
    }

    private MarsDbContext CreateContext() => _factory.CreateInstance();

    private static FeedbackEntity NewEntity(bool isRead, DateTimeOffset? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = createdAt ?? DateTimeOffset.Now,
        Title = "Test feedback",
        Content = "Content",
        FeedbackType = EFeedbackType.InfoMessage,
        IsRead = isRead,
    };

    private async Task<Guid> SeedEntity(bool isRead, DateTimeOffset? createdAt = null)
    {
        var entity = NewEntity(isRead, createdAt);
        await using var context = CreateContext();
        context.Feedbacks.Add(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    [Fact]
    public async Task Create_NewFeedback_IsReadFalseByDefault()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new FeedbackRepository(context);
        var query = new CreateFeedbackQuery
        {
            Title = "New feedback",
            Phone = null,
            Email = "test@example.com",
            Type = FeedbackType.InfoMessage.ToString(),
            FilledUsername = "Tester",
            Content = "Content",
            AuthorizedUserId = null,
        };

        // Act
        var id = await repository.Create(query, CancellationToken.None);

        // Assert
        var entity = await context.Feedbacks.FindAsync(id);
        entity!.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkRead_Unread_SetsIsReadAndReturnsTrue()
    {
        // Arrange
        var id = await SeedEntity(isRead: false);
        await using var context = CreateContext();
        var repository = new FeedbackRepository(context);

        // Act
        var changed = await repository.MarkRead(id, CancellationToken.None);

        // Assert
        changed.Should().BeTrue();
        var entity = await context.Feedbacks.FindAsync(id);
        entity!.IsRead.Should().BeTrue();
        entity.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkRead_AlreadyRead_ReturnsFalse()
    {
        // Arrange
        var id = await SeedEntity(isRead: true);
        await using var context = CreateContext();
        var repository = new FeedbackRepository(context);

        // Act
        var changed = await repository.MarkRead(id, CancellationToken.None);

        // Assert
        changed.Should().BeFalse();
    }

    [Fact]
    public async Task MarkRead_NotExist_ThrowsNotFoundException()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new FeedbackRepository(context);

        // Act
        var act = () => repository.MarkRead(Guid.NewGuid(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetDetail_MapsIsRead()
    {
        // Arrange
        var id = await SeedEntity(isRead: true);
        await using var context = CreateContext();
        var repository = new FeedbackRepository(context);

        // Act
        var detail = await repository.GetDetail(id, CancellationToken.None);

        // Assert
        detail!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task List_IsReadFilter_ReturnsMatchingOnly()
    {
        // Arrange
        var now = DateTimeOffset.Now;
        await SeedEntity(isRead: false, now.AddMinutes(-1));
        await SeedEntity(isRead: false, now.AddMinutes(-2));
        await SeedEntity(isRead: true, now.AddMinutes(-3));

        await using var context = CreateContext();
        var repository = new FeedbackRepository(context);

        // Act
        var unread = await repository.List(new ListFeedbackQuery { IsRead = false }, CancellationToken.None);
        var read = await repository.List(new ListFeedbackQuery { IsRead = true }, CancellationToken.None);
        var all = await repository.List(new ListFeedbackQuery(), CancellationToken.None);

        // Assert
        unread.Items.Should().HaveCount(2);
        unread.Items.Should().OnlyContain(s => !s.IsRead);
        read.Items.Should().HaveCount(1);
        read.Items.Should().OnlyContain(s => s.IsRead);
        all.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListTable_IsReadFilter_ReturnsMatchingOnly()
    {
        // Arrange
        var now = DateTimeOffset.Now;
        await SeedEntity(isRead: false, now.AddMinutes(-1));
        await SeedEntity(isRead: true, now.AddMinutes(-2));

        await using var context = CreateContext();
        var repository = new FeedbackRepository(context);

        // Act
        var unread = await repository.ListTable(new ListFeedbackQuery { IsRead = false }, CancellationToken.None);

        // Assert
        unread.Items.Should().HaveCount(1);
        unread.Items.Should().OnlyContain(s => !s.IsRead);
    }
}