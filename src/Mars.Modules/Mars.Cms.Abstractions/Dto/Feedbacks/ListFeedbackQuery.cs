using Mars.Contracts.Common;

namespace Mars.Cms.Abstractions.Dto.Feedbacks;

public record ListFeedbackQuery : BasicListQuery
{
    /// <summary>
    /// Фильтр по статусу прочтения: null — все, false — непрочитанные, true — прочитанные.
    /// </summary>
    public bool? IsRead { get; init; }
}
