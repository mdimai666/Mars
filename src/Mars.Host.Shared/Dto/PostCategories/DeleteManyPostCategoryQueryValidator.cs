using FluentValidation;
using Mars.Host.Shared.Services;

namespace Mars.Host.Shared.Dto.PostCategories;

public class DeleteManyPostCategoryQueryValidator : AbstractValidator<DeleteManyPostCategoryQuery>
{
    public DeleteManyPostCategoryQueryValidator(IPostCategoryMetaLocator postCategoryMetaLocator)
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("post type ids for delete must not be empty");

        RuleForEach(x => x.Ids)
            .SetValidator(new DeletePostCategoryQueryValidator(postCategoryMetaLocator));

        //Сделать чтобы было наоборот, чтобы Single вызывало Many [id]
    }
}
