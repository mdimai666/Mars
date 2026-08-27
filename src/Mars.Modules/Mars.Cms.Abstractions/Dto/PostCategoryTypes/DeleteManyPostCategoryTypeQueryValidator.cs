using FluentValidation;
using Mars.Cms.Abstractions.Repositories;

namespace Mars.Cms.Abstractions.Dto.PostCategoryTypes;

public class DeleteManyPostCategoryTypeQueryValidator : AbstractValidator<DeleteManyPostCategoryTypeQuery>
{
    public DeleteManyPostCategoryTypeQueryValidator(IPostCategoryTypeRepository userTypeRepository)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("PostCategoryType ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeletePostCategoryTypeQueryValidator(userTypeRepository));
    }
}
