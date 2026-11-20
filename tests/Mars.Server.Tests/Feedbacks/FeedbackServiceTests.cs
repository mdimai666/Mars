using FluentAssertions;
using Mars.Cms.Abstractions.Dto.Feedbacks;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Host.Services;
using Mars.Excel.Abstractions;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Mars.Server.Abstractions.Validators;
using NSubstitute;

namespace Mars.Server.Tests.Feedbacks;

public class FeedbackServiceTests
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IEventManager _eventManager;
    private readonly FeedbackService _feedbackService;

    public FeedbackServiceTests()
    {
        _feedbackRepository = Substitute.For<IFeedbackRepository>();
        _eventManager = Substitute.For<IEventManager>();
        _eventManager.Defaults.Returns(new EventManagerDefaults());

        _feedbackService = new FeedbackService(
            _feedbackRepository,
            _eventManager,
            Substitute.For<IExcelService>(),
            Substitute.For<IValidatorFactory>());
    }

    static FeedbackDetail CreateDetail(Guid id, bool isRead) => new()
    {
        Id = id,
        CreatedAt = DateTimeOffset.Now,
        Title = "Test feedback",
        Phone = null,
        Email = "test@example.com",
        Type = FeedbackType.InfoMessage.ToString(),
        IsAuthorizedUser = false,
        FilledUsername = "Tester",
        Content = "Content",
        IsRead = isRead,
    };

    [Fact]
    public async Task MarkRead_StatusChanged_TriggersFeedbackReadEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var detail = CreateDetail(id, isRead: true);
        _feedbackRepository.MarkRead(id, Arg.Any<CancellationToken>()).Returns(true);
        _feedbackRepository.GetDetail(id, Arg.Any<CancellationToken>()).Returns(detail);

        // Act
        var result = await _feedbackService.MarkRead(id, CancellationToken.None);

        // Assert
        result.Should().Be(detail);
        _eventManager.Received(1).TriggerEvent(Arg.Is<ManagerEventPayload>(p =>
            p.Topic == _eventManager.Defaults.FeedbackRead() && ReferenceEquals(p.Data, detail)));
    }

    [Fact]
    public async Task MarkRead_AlreadyRead_DoesNotTriggerEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var detail = CreateDetail(id, isRead: true);
        _feedbackRepository.MarkRead(id, Arg.Any<CancellationToken>()).Returns(false);
        _feedbackRepository.GetDetail(id, Arg.Any<CancellationToken>()).Returns(detail);

        // Act
        var result = await _feedbackService.MarkRead(id, CancellationToken.None);

        // Assert
        result.Should().Be(detail);
        _eventManager.DidNotReceive().TriggerEvent(Arg.Any<ManagerEventPayload>());
    }

    [Fact]
    public async Task MarkRead_UsesFeedbackReadTopic()
    {
        // Arrange
        var id = Guid.NewGuid();
        _feedbackRepository.MarkRead(id, Arg.Any<CancellationToken>()).Returns(true);
        _feedbackRepository.GetDetail(id, Arg.Any<CancellationToken>()).Returns(CreateDetail(id, isRead: true));

        // Act
        await _feedbackService.MarkRead(id, CancellationToken.None);

        // Assert
        _eventManager.Received(1).TriggerEvent(Arg.Is<ManagerEventPayload>(p => p.Topic == "entity/feedback/read"));
    }
}