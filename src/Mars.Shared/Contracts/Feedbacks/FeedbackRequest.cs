using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Mars.Shared.Common;

namespace Mars.Shared.Contracts.Feedbacks;

public record CreateFeedbackRequest
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public required string Title { get; init; }
    [DefaultValue(null)]
    public required string? Phone { get; init; }

    [EmailAddress]
    [DefaultValue(null)]
    public required string? Email { get; init; }

    //See FeedbackType
    [Required]
    [StringLength(100, MinimumLength = 3)]
    [DefaultValue("InfoMessage")]
    public required string Type { get; init; }
    public required string FilledUsername { get; init; }

    [StringLength(1000, MinimumLength = 3)]
    public required string Content { get; init; }

}

public record UpdateFeedbackRequest
{
    public required Guid Id { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Type { get; init; }
}

public record ListFeedbackQueryRequest : BasicListQueryRequest
{
}

public record TableFeedbackQueryRequest : BasicTableQueryRequest
{
}
