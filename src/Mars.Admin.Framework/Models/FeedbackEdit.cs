using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Feedbacks;

namespace AppFront.Shared.Models;

public class FeedbackEdit
{
    [StringLength(255, MinimumLength = 3)]
    public string Title { get; set; } = default!;
    public string? Phone { get; set; } = default!;
    public string? Email { get; set; } = default!;
    [StringLength(100, MinimumLength = 3)]
    public string Type { get; set; } = default!;
    public string FilledUsername { get; set; } = default!;

    [StringLength(1000, MinimumLength = 3)]
    public string Content { get; set; } = default!;

    public CreateFeedbackRequest ToRequest()
        => new()
        {
            Title = Title,
            Phone = Phone,
            Email = Email,
            Type = Type,
            FilledUsername = FilledUsername,
            Content = Content,
        };
}
