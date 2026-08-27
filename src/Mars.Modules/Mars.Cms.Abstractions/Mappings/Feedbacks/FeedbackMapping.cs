using Mars.Data.Extensions;
using Mars.Cms.Abstractions.Dto.Feedbacks;
using Mars.Contracts.Common;
using Mars.Contracts.Feedbacks;

namespace Mars.Cms.Abstractions.Mappings.Feedbacks;

public static class FeedbackMapping
{
    public static FeedbackSummaryResponse ToResponse(this FeedbackSummary entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.Type,
            Email = entity.Email,
            Phone = entity.Phone,
            Excerpt = entity.Excerpt,
            FilledUsername = entity.FilledUsername,
            IsAuthorizedUser = entity.IsAuthorizedUser,
        };

    public static FeedbackDetailResponse ToResponse(this FeedbackDetail entity)
      => new()
      {
          Id = entity.Id,
          CreatedAt = entity.CreatedAt,
          Title = entity.Title,
          Type = entity.Type,
          Email = entity.Email,
          Phone = entity.Phone,
          Content = entity.Content,
          FilledUsername = entity.FilledUsername,
          IsAuthorizedUser = entity.IsAuthorizedUser,
      };

    public static ListDataResult<FeedbackSummaryResponse> ToResponse(this ListDataResult<FeedbackSummary> list)
        => list.ToMap(ToResponse);

    public static PagingResult<FeedbackSummaryResponse> ToResponse(this PagingResult<FeedbackSummary> list)
        => list.ToMap(ToResponse);
}
