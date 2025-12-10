using FluentValidation;
using Mars.Core.Features;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

public class GeneralPostTypeQueryValidator : AbstractValidator<IGeneralPostTypeQuery>
{
    public GeneralPostTypeQueryValidator()
    {
        RuleFor(x => x.TypeName)
            .NotEmpty()
            .MinimumLength(PostTypeConstants.TypeNameMinLength)
            .MaximumLength(PostTypeConstants.TypeNameMaxLength);

        RuleFor(x => x.TypeName)
            .Must(TextTool.IsValidSlugWithUpperCase)
            .WithMessage(v => $"'{v.TypeName}' is Invalid TypeName");
    }
}

public interface IGeneralPostTypeQuery
{
    string Title { get; }
    string TypeName { get; }
}
