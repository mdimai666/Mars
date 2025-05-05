using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.Feedbacks;

namespace Mars.Host.Shared.Dto.Feedbacks;

public static class FeedbackRequestExtensions
{
    public static CreateFeedbackQuery ToQuery(this CreateFeedbackRequest request, Guid? authorizedUserId)
        => new()
        {
            Title = request.Title,
            Type = request.Type,
            Phone = request.Phone,
            Email = request.Email,
            Content = request.Content,
            FilledUsername = request.FilledUsername,
            AuthorizedUserId = authorizedUserId
        };
    public static UpdateFeedbackQuery ToQuery(this UpdateFeedbackRequest request)
        => new()
        {
            Id = request.Id,
            Type = request.Type,
        };

    public static ListFeedbackQuery ToQuery(this ListFeedbackQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListFeedbackQuery ToQuery(this TableFeedbackQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static FeedbackType? ParseFeedbackType(string feedbackType)
        => Enum.TryParse<FeedbackType>(feedbackType, out var type) ? type : null;
}
