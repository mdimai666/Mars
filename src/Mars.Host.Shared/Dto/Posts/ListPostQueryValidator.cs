using Mars.Core.Constants;
using Mars.Host.Shared.Services;
using FluentValidation;

namespace Mars.Host.Shared.Dto.Posts;

public class ListPostQueryValidator : AbstractValidator<ListPostQuery>
{
    public ListPostQueryValidator(IMetaModelTypesLocator metaModelTypesLocator)
    {
        RuleFor(x => x.Type)
            //.NotEmpty()
            .Must(v => v == null || metaModelTypesLocator.GetPostTypeByName(v) != null)
            .WithErrorCode(nameof(HttpConstants.UserActionErrorCode466))
            .WithMessage(v => $"post type '{v.Type}' not exist");
    }
}
