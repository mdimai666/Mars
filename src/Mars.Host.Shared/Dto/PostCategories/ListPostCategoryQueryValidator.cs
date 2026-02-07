using FluentValidation;
using Mars.Core.Constants;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategories;

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
