using System.ComponentModel.DataAnnotations;
using Mars.Shared.Contracts.Feedbacks;

namespace AppAdmin.Pages.FeedbackViews;

public class CreateFeedbackModel
{
    [Required]
    [StringLength(255, MinimumLength = 3)]
    public string Title { get; set; } = "";
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    //See FeedbackType
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Type { get; set; } = "InfoMessage";
    public string FilledUsername { get; set; } = "";

    [StringLength(1000, MinimumLength = 3)]
    public string Content { get; set; } = "";

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
