using FluentValidation;
using Mars.Core.Constants;
using Mars.Cms.Abstractions.Services;

namespace Mars.Cms.Abstractions.Dto.PostCategories;

public class ListPostCategoryQueryValidator : AbstractValidator<ListPostCategoryQuery>
{
    public ListPostCategoryQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator)
    {
        RuleFor(x => x.Type)
            //.NotEmpty()
            .Must(v => v == null || postCategoryMetaLocator.GetTypeDetailByName(v) != null)
            .WithErrorCode(nameof(HttpConstants.UserActionErrorCode466))
            .WithMessage(v => $"postCategory type '{v.Type}' not exist");
    }
}
