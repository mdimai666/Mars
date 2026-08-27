using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Feedbacks;

namespace Mars.Host.Repositories.Mappings;

internal static class FeedbackMapping
{
    public static FeedbackSummary ToSummary(this FeedbackEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.FeedbackType.ToString(),
            Email = entity.Email ?? entity.AuthorizedUser?.Email,
            Phone = entity.Phone ?? entity.AuthorizedUser?.PhoneNumber,
            Excerpt = entity.Content?.TextEllipsis(150) ?? "",
            FilledUsername = entity.FilledUsername ?? entity.AuthorizedUser?.FullName ?? "",
            IsAuthorizedUser = entity.AuthorizedUserId != null,
        };

    public static FeedbackDetail ToDetail(this FeedbackEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.FeedbackType.ToString(),
            Email = entity.Email ?? entity.AuthorizedUser?.Email,
            Phone = entity.Phone ?? entity.AuthorizedUser?.PhoneNumber,
            FilledUsername = entity.FilledUsername ?? entity.AuthorizedUser?.FullName ?? "",
            IsAuthorizedUser = entity.AuthorizedUserId != null,
            Content = entity.Content ?? "",
        };

    public static IReadOnlyCollection<FeedbackSummary> ToSummaryList(this IEnumerable<FeedbackEntity> entities)
        => entities.Select(ToSummary).ToArray();

    public static IReadOnlyCollection<FeedbackDetail> ToDetailList(this IEnumerable<FeedbackEntity> entities)
        => entities.Select(ToDetail).ToArray();
}
