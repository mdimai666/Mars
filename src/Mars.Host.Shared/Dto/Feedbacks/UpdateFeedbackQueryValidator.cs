using Mars.Core.Constants;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Feedbacks;

public class UpdateFeedbackQueryValidator : AbstractValidator<UpdateFeedbackQuery>
{
    public UpdateFeedbackQueryValidator()
    {
        var allowedTypes = Enum.GetValues<FeedbackType>().Select(s => s.ToString()).ToArray();

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(v => allowedTypes.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode(nameof(HttpConstants.UserActionErrorCode466))
            .WithMessage(v => $"feedback type '{v.Type}' is not allowed");
    }
}
